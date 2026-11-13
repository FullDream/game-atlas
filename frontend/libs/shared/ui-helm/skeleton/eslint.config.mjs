import nx from '@nx/eslint-plugin'
import baseConfig from '../../../../eslint.config.mjs'

export default [
	...nx.configs['flat/angular'],
	...nx.configs['flat/angular-template'],
	...baseConfig,
	{
		files: ['**/*.ts'],
		rules: {
			'@angular-eslint/directive-selector': [
				'error',
				{
					type: 'attribute',
					prefix: ['hlm', 'ui'],
					style: 'camelCase',
				},
			],
			'@angular-eslint/component-selector': [
				'error',
				{
					type: 'element',
					prefix: ['hlm', 'ui', 'sim'],
					style: 'kebab-case',
				},
			],
			'@angular-eslint/no-input-rename': 'off',
			'@nx/enforce-module-boundaries': (() => {
				const r = baseConfig.find(c => c.rules && c.rules['@nx/enforce-module-boundaries'])?.rules[
					'@nx/enforce-module-boundaries'
				]

				return r ? [r[0], { ...r[1], allowCircularSelfDependency: true }] : undefined
			})(),
			'@angular-eslint/directive-class-suffix': 'off',
			'@angular-eslint/component-class-suffix': 'off',
			'@typescript-eslint/naming-convention': baseConfig.find(
				c => c.rules?.['@typescript-eslint/naming-convention'],
			).rules['@typescript-eslint/naming-convention'],
		},
	},
	{
		files: ['**/carousel/src/lib/hlm-carousel-next.ts', '**/carousel/src/lib/hlm-carousel-previous.ts'],
		rules: {
			'@angular-eslint/component-selector': ['error', { type: 'attribute', prefix: 'hlm', style: 'camelCase' }],
		},
	},
	{
		files: ['**/*.html'],
		// Override or add rules here
		rules: {
			'@angular-eslint/template/interactive-supports-focus': 'off',
			'@angular-eslint/template/click-events-have-key-events': 'off',
		},
	},
]
