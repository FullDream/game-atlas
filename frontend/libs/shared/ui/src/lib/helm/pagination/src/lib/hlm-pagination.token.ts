import { inject, InjectionToken, type ValueProvider } from '@angular/core'

export type HlmPaginationConfig = {
	totalItemLabel: string
	totalItemsLabel: string
	pageLabel: string
	pagesLabel: string
	previousLabel: string
	previousAriaLabel: string
	nextLabel: string
	nextAriaLabel: string
}

const defaultConfig: HlmPaginationConfig = {
	totalItemLabel: 'total item',
	totalItemsLabel: 'total items',
	pageLabel: 'page',
	pagesLabel: 'pages',
	nextLabel: 'Next',
	nextAriaLabel: 'Go to next page',
	previousLabel: 'Previous',
	previousAriaLabel: 'Go to previous page',
}

const HlmPaginationConfigToken = new InjectionToken<HlmPaginationConfig>('HlmPaginationConfig')

export function provideHlmPaginationConfig(config: Partial<HlmPaginationConfig>): ValueProvider {
	return { provide: HlmPaginationConfigToken, useValue: { ...defaultConfig, ...config } }
}

export function injectHlmPaginationConfig(): HlmPaginationConfig {
	return inject(HlmPaginationConfigToken, { optional: true }) ?? defaultConfig
}
