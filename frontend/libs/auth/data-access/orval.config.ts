import { resolve } from 'node:path'
import { defineConfig } from 'orval'

const generated = resolve('libs/auth/data-access/src/lib/generated')

export default defineConfig({
	auth: {
		input: {
			target: process.env['GAME_ATLAS_OPENAPI_URL'] ?? 'http://localhost:5041/openapi/v1.json',
			filters: { tags: ['Auth'] },
		},
		output: {
			target: resolve(generated, 'api.ts'),
			schemas: resolve(generated, 'models'),
			client: 'angular',
			mode: 'tags-split',
			namingConvention: 'kebab-case',
			clean: [generated],
			override: {
				useTypeOverInterfaces: true,
				enumGenerationType: 'union',
				angular: {
					retrievalClient: 'httpClient',
					baseUrl: { apiId: 'auth' },
				},
			},
		},
		hooks: { afterAllFilesWrite: 'prettier --write' },
	},
})
