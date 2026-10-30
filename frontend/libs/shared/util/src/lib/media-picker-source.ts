import { InjectionToken } from '@angular/core'
import type { HttpErrorResponse } from '@angular/common/http'
import type { CreateQueryOptions } from '@tanstack/angular-query-experimental'
export type LibraryImage = {
	type: 'image'
	id: string
	name: string
	url: string
	alt: string
	placeholder: string | null
	width: number | null
	height: number | null
	folderId: string | null
	contentType: string
	byteLength: number
	createdAt: string
	updatedAt: string
}
export type LibraryFolder = { type: 'folder'; id: string; name: string }
export type LibraryEntry = LibraryImage | LibraryFolder
export type LibraryPage = { items: LibraryEntry[]; offset: number; limit: number; total: number }
export type LibraryParams = { folderId?: string; offset?: number; limit?: number; search?: string }
export type PickerContext = { mode: 'image' }
type PickerQuery<T> = Pick<CreateQueryOptions<T, HttpErrorResponse>, 'queryKey' | 'queryFn'>
export type MediaPickerSource = {
	list: (params: LibraryParams) => PickerQuery<LibraryPage>
	errorMessage: (error: unknown) => string
}
export const MEDIA_PICKER_SOURCE = new InjectionToken<MediaPickerSource>('Media picker source')
