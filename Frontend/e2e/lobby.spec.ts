import { expect, test } from '@playwright/test';

test('nickname, password, host controls, live poker and refresh recovery', { tag: '@integration' }, async ({ browser }) => {
  const hostContext = await browser.newContext();
  const guestContext = await browser.newContext();
  const host = await hostContext.newPage();
  const guest = await guestContext.newPage();
  const errors: string[] = [];
  host.on('pageerror', error => errors.push(error.message));
  guest.on('pageerror', error => errors.push(error.message));
  try {
    for (const [page, name] of [[host, 'Alice'], [guest, 'Bob']] as const) {
      await page.goto('/');
      await page.getByLabel('Nickname', { exact: true }).fill(name);
      await page.getByRole('button', { name: 'Look for lobbies' }).click();
      await expect(page.getByRole('heading', { name: 'Open lobbies' })).toBeVisible();
    }
    await host.getByLabel('Lobby name', { exact: true }).fill('Browser test table');
    await host.getByLabel('Lobby password', { exact: true }).fill('secret');
    await host.getByLabel('Maximum players').fill('10');
    await host.getByRole('button', { name: 'Create lobby', exact: true }).click();
    await expect(host.getByRole('button', { name: 'Start game' })).toBeDisabled();
    await guest.getByRole('listitem').filter({ hasText: 'Browser test table' }).getByRole('button', { name: 'Join', exact: true }).click();
    await guest.getByRole('dialog').getByLabel('Lobby password').fill('wrong');
    await guest.getByRole('button', { name: 'Join lobby', exact: true }).click();
    await expect(guest.getByRole('dialog')).toContainText('Incorrect lobby password');
    await guest.getByRole('dialog').getByLabel('Lobby password').fill('secret');
    await guest.getByRole('button', { name: 'Join lobby', exact: true }).click();
    await expect(host.getByRole('button', { name: 'Kick Bob' })).toBeVisible();
    await expect(guest.getByRole('button', { name: 'Start game' })).toHaveCount(0);
    await host.getByRole('button', { name: 'Kick Bob' }).click();
    await expect(guest.getByRole('heading', { name: 'Open lobbies' })).toBeVisible();
    await guest.getByRole('listitem').filter({ hasText: 'Browser test table' }).getByRole('button', { name: 'Join', exact: true }).click();
    await guest.getByRole('dialog').getByLabel('Lobby password').fill('secret');
    await guest.getByRole('button', { name: 'Join lobby', exact: true }).click();
    await expect(host.getByRole('button', { name: 'Start game' })).toBeEnabled();
    await host.getByRole('button', { name: 'Start game' }).click();
    for (const page of [host, guest]) {
      await expect(page.getByRole('heading', { name: 'Poker table' })).toBeVisible();
      await expect(page.locator('.seat')).toHaveCount(2);
    }
    await expect(host.getByLabel("Bob's cards")).toHaveText('??');
    await expect(guest.getByLabel("Alice's cards")).toHaveText('??');
    await host.getByRole('button', { name: 'Fold', exact: true }).click();
    await expect(host.getByRole('button', { name: 'Deal next hand' })).toBeVisible();
    await host.getByRole('button', { name: 'Deal next hand' }).click();
    await guest.reload();
    await expect(guest.getByRole('heading', { name: 'Poker table' })).toBeVisible();
    await expect(guest.getByText('Game in progress', { exact: true })).toBeVisible();
    await guest.getByRole('button', { name: 'Leave lobby' }).click();
    await guest.getByRole('dialog').getByRole('button', { name: 'Leave', exact: true }).click();
    await expect(guest.getByRole('heading', { name: 'Open lobbies' })).toBeVisible();
    expect(await guest.evaluate(() => localStorage.getItem('poker.nickname'))).toBe('Bob');
    expect(errors).toEqual([]);
  } finally { await hostContext.close(); await guestContext.close(); }
});
