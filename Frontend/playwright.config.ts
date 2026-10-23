import { defineConfig } from '@playwright/test';
import { existsSync } from 'node:fs';

const dotnet = process.env.DOTNET_ROOT ? `${process.env.DOTNET_ROOT}/dotnet` : 'dotnet';
const executablePath = process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE || (existsSync('/usr/bin/chromium') ? '/usr/bin/chromium' : undefined);
export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  use: { baseURL: 'http://127.0.0.1:5180', headless: true, launchOptions: { executablePath } },
  webServer: [
    { command: `${dotnet} run --project ../Api/Api.csproj --no-launch-profile --urls http://127.0.0.1:5080`, url: 'http://127.0.0.1:5080/api/health', timeout: 120_000 },
    { command: 'npm run dev -- --host 127.0.0.1 --port 5180', url: 'http://127.0.0.1:5180', timeout: 60_000 },
  ],
});
