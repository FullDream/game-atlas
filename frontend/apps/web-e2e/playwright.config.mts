import { defineConfig, devices } from '@playwright/test'
import { nxE2EPreset } from '@nx/playwright/preset'
import { workspaceRoot } from '@nx/devkit'

// For CI, you may want to set BASE_URL to the deployed application.
const baseURL = process.env['BASE_URL'] || 'http://localhost:4200'

// The .mts extension lets both Playwright and Nx load this configuration as ESM.
export default defineConfig({
	...nxE2EPreset(import.meta.dirname, { testDir: './src' }),
	use: {
		baseURL,
		trace: 'on-first-retry',
	},
	webServer: {
		command: 'npx nx run web:serve-static',
		url: 'http://localhost:4200',
		timeout: 180_000,
		reuseExistingServer: true,
		cwd: workspaceRoot,
	},
	projects: [
		{
			name: 'chromium',
			use: { ...devices['Desktop Chrome'] },
		},

		{
			name: 'firefox',
			use: { ...devices['Desktop Firefox'] },
		},

		{
			name: 'webkit',
			use: { ...devices['Desktop Safari'] },
		},
	],
})
