export const folderIdQueryParamOptions = {
	parse: (value: string | null): string | null =>
		value && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) ? value : null,
}

export const offsetQueryParamOptions = {
	parse: (value: string | null): number => {
		const offset = Number(value ?? 0)

		return Number.isSafeInteger(offset) && offset >= 0 && offset <= 2147483647 ? offset : 0
	},
	stringify: (value: number) => (value === 0 ? null : value),
}

export const limitQueryParamOptions = {
	parse: (value: string | null): number => {
		const limit = Number(value ?? 24)

		return Number.isSafeInteger(limit) && limit >= 24 && limit <= 120 && limit % 24 === 0 ? limit : 24
	},
	stringify: (value: number) => (value === 24 ? null : value),
}

export const searchQueryParamOptions = {
	parse: (value: string | null) => value ?? '',
	stringify: (value: string) => value || null,
}
