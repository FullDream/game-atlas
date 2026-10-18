import { test, expect } from '@playwright/test'

test('opens the catalog and hydrates without fetching the JSON again', async ({ page, request }) => {
	const response = await request.get('/content/games.json')

	expect(response.ok()).toBe(true)
	const games: { id: string; title: string }[] = await response.json()
	const catalogRequests: string[] = []

	page.on('request', request => {
		if (new URL(request.url()).pathname === '/content/games.json') catalogRequests.push(request.url())
	})
	await page.goto('/')
	await expect(page).toHaveTitle('Games | Game Atlas')
	await expect(page.getByRole('heading', { name: 'Games', exact: true })).toBeVisible()
	await expect(page.getByTestId('game-card')).toHaveCount(games.length)
	for (const game of games) {
		await expect(page.getByRole('heading', { name: game.title, exact: true })).toBeVisible()
	}

	// Wait for the prerendered feature to be claimed by Angular hydration.
	await expect(page.locator('lib-games-feature-catalog')).not.toHaveAttribute('ngh', /.+/)
	expect(catalogRequests).toHaveLength(0)
	await expect(page.getByTestId('game-card').getByRole('link')).toHaveCount(0)
})

test('fits the catalog into a mobile viewport', async ({ page }, testInfo) => {
	await page.setViewportSize({ width: 390, height: 844 })
	await page.goto('/')
	await expect(page.getByRole('heading', { name: 'Games', exact: true })).toBeVisible()
	const card = page.getByTestId('game-card').first()

	await expect(card).toBeVisible()
	const bounds = await card.boundingBox()

	expect(bounds).not.toBeNull()
	expect(bounds?.x).toBeGreaterThanOrEqual(0)
	expect((bounds?.x ?? 0) + (bounds?.width ?? 0)).toBeLessThanOrEqual(390)
	expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
	await page.screenshot({ path: testInfo.outputPath('catalog-mobile.png'), fullPage: true })
})

test.describe('static output', () => {
	test.use({ javaScriptEnabled: false })

	test('serves the prerendered home page and public assets', async ({ page, request }) => {
		const response = await page.goto('/')

		expect(response?.status()).toBe(200)
		await expect(page.locator('h1')).toHaveText('Games')
		await expect(page.getByTestId('game-card').getByRole('heading', { name: 'Elden Ring' })).toBeVisible()

		const favicon = await request.get('/favicon.ico')

		expect(favicon.status()).toBe(200)
		expect(favicon.headers()['content-type']).toMatch(/image\//)
	})
})
