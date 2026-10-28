export type WorkspaceBreadcrumb = {
	label: string
	url?: string
	queryParams?: Record<string, string | number | null>
	drop?: { accepts: (id: string) => boolean; perform: (id: string) => void }
}
