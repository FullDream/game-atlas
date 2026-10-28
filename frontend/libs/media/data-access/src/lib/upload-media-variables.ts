import type { MutationStatus, Mutation, MutationFilters } from '@tanstack/angular-query-experimental'

export type UploadMediaVariables = {
	key: string
	file: File
	folderId: string | null
	controller: AbortController
}

export type MediaUploadMutation = {
	remove: () => void
	submittedAt: number
	variables: UploadMediaVariables
	status: MutationStatus
	error: unknown
}

export type MediaUploadStateOptions = {
	filters: MutationFilters
	select: (mutation: Mutation) => MediaUploadMutation
}
