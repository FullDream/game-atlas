import { test, expect } from '@playwright/test'

test('serves the neutral public entry without the removed catalog', async ({ page }) => {
	await page.goto('/')
	await expect(page.getByRole('heading', { name: 'GameAtlas', exact: true })).toBeVisible()
})
