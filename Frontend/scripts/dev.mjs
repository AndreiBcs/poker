import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';
import { setTimeout } from 'node:timers/promises';

const children = [];
let stopping = false;
function stop(code = 0) {
  if (stopping) return;
  stopping = true;
  for (const child of children) child.kill('SIGTERM');
  process.exitCode = code;
}
function start(command, args) {
  const child = spawn(command, args, { stdio: 'inherit', cwd: new URL('..', import.meta.url) });
  children.push(child);
  child.on('error', error => { console.error(error.message); stop(1); });
  child.on('exit', code => { if (!stopping) stop(code || 1); });
  return child;
}
process.on('SIGINT', () => stop());
process.on('SIGTERM', () => stop());

// Resolve relative to this file so npm --prefix and direct invocation both work.
const apiProject = fileURLToPath(new URL('../../Api/Api.csproj', import.meta.url));
const dotnet = process.env.DOTNET_ROOT ? `${process.env.DOTNET_ROOT}/dotnet` : 'dotnet';
start(dotnet, ['run', '--project', apiProject, '--no-launch-profile', '--urls', 'http://127.0.0.1:5080']);
const deadline = Date.now() + 120_000;
let ready = false;
while (!stopping && Date.now() < deadline) {
  try {
    const response = await fetch('http://127.0.0.1:5080/api/health', { signal: AbortSignal.timeout(1000) });
    ready = response.ok && (await response.json()).status === 'ok';
    if (ready) break;
  } catch { /* API may still be compiling. */ }
  await setTimeout(250);
}
if (!stopping) {
  if (!ready) { console.error('API did not become ready on port 5080.'); stop(1); }
  else start(process.execPath, [fileURLToPath(new URL('../node_modules/vite/bin/vite.js', import.meta.url)), ...process.argv.slice(2)]);
}
