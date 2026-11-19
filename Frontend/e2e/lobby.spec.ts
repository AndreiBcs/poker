import { expect, test } from '@playwright/test';

for (const [status, message] of [
  [404, 'The running Poker API is out of date.'],
  [401, 'Poker could not verify your browser seat.'],
] as const) {
  test(`nickname registration keeps the lobby closed when seat verification returns ${status}`, { tag: '@integration' }, async ({ page }) => {
    await page.route('**/api/players/me', (route) => route.fulfill({ status }));
    await page.route('**/api/players', (route) => route.fulfill({
      json: { id: 'test-seat', nickname: 'Alice' },
    }));
    await page.goto('/');
    await page.getByLabel('Nickname', { exact: true }).fill('Alice');
    await page.getByRole('button', { name: 'Look for lobbies' }).click();
    await expect(page.getByRole('alert')).toContainText(message);
    await expect(page.getByRole('heading', { name: 'Open lobbies' })).toHaveCount(0);
  });
}

test('lobby controls, live poker, reconnect and leave', { tag: '@integration' }, async ({ browser }) => {
  const hostContext = await browser.newContext();
  const guestContext = await browser.newContext();
  const host = await hostContext.newPage();
  const guest = await guestContext.newPage();
  const errors: string[] = [];
  let disconnectGuest: (() => Promise<void>) | undefined;
  await guest.routeWebSocket(/\/api\/live\?/, (socket) => {
    const server = socket.connectToServer();
    disconnectGuest = async () => {
      await Promise.all([socket.close(), server.close()]);
    };
  });
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
    await expect(guest.getByRole('alert')).toContainText('You are no longer in this lobby.');
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

    const savedSeat = await guest.evaluate(() => sessionStorage.getItem('poker.player'));
    expect(savedSeat).toBeTruthy();
    const negotiate = '**/api/live/negotiate?*';
    await guest.route(negotiate, (route) => route.abort());
    const failedRetry = guest.waitForEvent('requestfailed', {
      predicate: (request) => new URL(request.url()).pathname === '/api/live/negotiate',
    });
    expect(disconnectGuest).toBeDefined();
    await disconnectGuest!();
    await expect(guest.getByText('Reconnecting…', { exact: true })).toBeVisible();
    await expect(host.getByText('Game paused: waiting for players to reconnect.')).toBeVisible();
    await failedRetry;
    await expect(guest.getByRole('alert')).toHaveCount(0);
    await guest.unroute(negotiate);
    await expect(guest.getByText('Game in progress', { exact: true })).toBeVisible();
    expect(await guest.evaluate(() => sessionStorage.getItem('poker.player'))).toBe(savedSeat);

    await guest.getByRole('button', { name: 'Raise', exact: true }).click();
    const raiseSlider = guest.getByRole('slider', { name: 'Chips to put in' });
    const exactRaise = guest.getByRole('spinbutton', { name: 'Exact chips to put in' });
    const confirmRaise = guest.getByRole('button', { name: 'Confirm raise' });
    await expect(raiseSlider).toHaveValue('75');
    await raiseSlider.press('Home');
    await expect(raiseSlider).toHaveValue('0');
    await expect(confirmRaise).toBeDisabled();
    const track = (await raiseSlider.boundingBox())!;
    await guest.mouse.move(track.x + 8, track.y + track.height / 2);
    await guest.mouse.down();
    await guest.mouse.move(track.x + track.width - 1, track.y + track.height / 2, { steps: 8 });
    await guest.mouse.up();
    await expect(raiseSlider).toHaveValue('2000');
    await expect(exactRaise).toHaveValue('2000');
    await expect(confirmRaise).toBeEnabled();
    for (const invalid of ['', '76.5', '2001']) {
      await exactRaise.fill(invalid);
      await expect(confirmRaise).toBeDisabled();
    }
    await exactRaise.fill('76');
    await expect(raiseSlider).toHaveValue('76');
    await confirmRaise.click();
    await host.getByRole('button', { name: 'Raise', exact: true }).click();
    const exactReraise = host.getByRole('spinbutton', { name: 'Exact chips to put in' });
    await expect(exactReraise).toHaveValue('102');
    await expect(host.getByRole('dialog')).toContainText('Minimum full raise to 152 takes 102 chips');
    await exactReraise.fill('120');
    await expect(host.getByRole('dialog')).toContainText('Raise to 170');
    await host.getByRole('button', { name: 'Confirm raise' }).click();
    await guest.getByRole('button', { name: 'Call', exact: true }).click();
    const hostBoard = host.getByLabel('Community cards').locator('.card');
    const guestBoard = guest.getByLabel('Community cards').locator('.card');
    let finalBoard: string[] = [];
    for (const cardCount of [3, 4, 5]) {
      await expect(hostBoard).toHaveCount(cardCount);
      finalBoard = await hostBoard.allTextContents();
      await expect(guestBoard).toHaveText(finalBoard);
      await host.getByRole('button', { name: 'Check', exact: true }).click();
      await guest.getByRole('button', { name: 'Check', exact: true }).click();
    }
    await expect(host.getByRole('button', { name: 'Deal next hand' })).toBeEnabled();
    await expect(host.getByText('Final pot: 340', { exact: true })).toBeVisible();
    await expect(guest.getByText('Final pot: 340', { exact: true })).toBeVisible();
    await expect(hostBoard).toHaveText(finalBoard);
    await expect(guestBoard).toHaveText(finalBoard);
    await host.getByRole('button', { name: 'Deal next hand' }).click();
    await expect(hostBoard).toHaveCount(0);
    await expect(guestBoard).toHaveCount(0);

    await guest.getByRole('button', { name: 'Leave lobby' }).click();
    await guest.route('**/api/lobbies/*/step-away', (route) => route.fulfill({ status: 503 }), {
      times: 1,
    });
    const leaveDialog = guest.getByRole('dialog');
    await leaveDialog.getByRole('button', { name: 'Leave', exact: true }).click();
    await expect(leaveDialog).toContainText('The game server is unavailable.');
    await expect(guest.getByRole('heading', { name: 'Poker table' })).toBeVisible();
    await guest.getByRole('dialog').getByRole('button', { name: 'Leave', exact: true }).click();
    await expect(guest.getByRole('heading', { name: 'Open lobbies' })).toBeVisible();
    await expect(guest.getByRole('alert')).toHaveCount(0);
    expect(await guest.evaluate(() => sessionStorage.getItem('poker.room'))).toBeNull();
    expect(await guest.evaluate(() => localStorage.getItem('poker.nickname'))).toBe('Bob');
    await expect(guest.getByRole('heading', { name: 'Saved game' })).toBeVisible();
    await expect(guest.getByRole('button', { name: 'Create lobby', exact: true })).toBeDisabled();
    const savedGame = await guest.evaluate(() => JSON.parse(localStorage.getItem('poker.game')!));
    expect(savedGame).toMatchObject({ gameId: expect.any(String), sessionId: expect.any(String) });
    expect((await guestContext.cookies()).find((cookie) => cookie.name === 'poker.seat')?.httpOnly).toBe(true);

    await guest.close();
    const returningGuest = await guestContext.newPage();
    await returningGuest.goto('/');
    await expect(returningGuest.getByRole('heading', { name: 'Saved game' })).toBeVisible();
    await returningGuest.getByRole('button', { name: 'Reconnect', exact: true }).click();
    await expect(returningGuest.getByRole('heading', { name: 'Poker table' })).toBeVisible();
    await expect(returningGuest.getByText('Game in progress', { exact: true })).toBeVisible();
    expect(await returningGuest.evaluate(() => sessionStorage.getItem('poker.player'))).toBe(savedSeat);

    for (const page of [returningGuest, host]) {
      await page.getByRole('button', { name: 'Leave lobby' }).click();
      await page.getByRole('dialog').getByRole('button', { name: 'Leave', exact: true }).click();
      await expect(page.getByRole('heading', { name: 'Saved game' })).toBeVisible();
    }
    await returningGuest.getByRole('button', { name: 'Abandon', exact: true }).click();
    await returningGuest.getByRole('dialog').getByRole('button', { name: 'Abandon game' }).click();
    for (const page of [returningGuest, host]) {
      await expect(page.getByRole('heading', { name: 'Saved game' })).toHaveCount(0);
      await expect(page.getByRole('button', { name: 'Create lobby', exact: true })).toBeEnabled();
      expect(await page.evaluate(() => localStorage.getItem('poker.game'))).toBeNull();
    }
    expect(errors).toEqual([]);
  } finally { await hostContext.close(); await guestContext.close(); }
});
