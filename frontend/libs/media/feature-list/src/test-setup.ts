beforeEach(() => {
	vi.stubGlobal(
		'ResizeObserver',
		class {
			observe = vi.fn()
			unobserve = vi.fn()
			disconnect = vi.fn()
		},
	)
	vi.stubGlobal(
		'matchMedia',
		vi.fn((query: string) => ({
			matches: query.includes('min-width'),
			media: query,
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			addListener: vi.fn(),
			removeListener: vi.fn(),
		})),
	)
})

afterEach(() => {
	vi.unstubAllGlobals()
	vi.restoreAllMocks()
})
