import { Component, computed, inject, signal } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { MediaQueryOptions, getMediaErrorMessage, type MediaItem } from '@game-atlas/media/data-access'
import { Button } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matFolderRound,
	matSearchRound,
	matCloseRound,
	matChevronLeftRound,
	matChevronRightRound,
} from '@ng-icons/material-symbols/round'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmAttachmentImports } from '@spartan-ng/helm/attachment'
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog'
import { injectQuery, injectMutation } from '@tanstack/angular-query-experimental'
type FolderPickerContext = { item: MediaItem; folderId: string | null }
@Component({
	selector: 'media-folder-picker-dialog',
	imports: [Button, FormsModule, NgIcon, HlmInput, HlmAttachmentImports, HlmSkeleton],
	templateUrl: './media-picker-dialog.html',
	styleUrl: './media-picker-dialog.css',
	providers: [
		provideIcons({ matFolderRound, matSearchRound, matCloseRound, matChevronLeftRound, matChevronRightRound }),
	],
	host: {
		class: 'flex max-h-[90dvh] w-[min(52rem,calc(100vw-2rem))] flex-col overflow-hidden rounded-xl border bg-background text-foreground shadow-xl',
	},
})
export class MediaPickerDialog {
	protected readonly context = injectBrnDialogContext<FolderPickerContext>()
	protected readonly dialog = inject(BrnDialogRef)
	readonly #options = inject(MediaQueryOptions)
	protected readonly folderId = signal(this.context.folderId)
	protected readonly offset = signal(0)
	protected readonly search = signal('')
	protected readonly skeletons = Array.from({ length: 12 }, (value, index) => index)
	protected readonly mediaQuery = injectQuery(() =>
		this.#options.folderChoices({
			folderId: this.folderId() ?? undefined,
			offset: this.offset(),
			limit: 24,
			search: this.search(),
		}),
	)
	protected readonly folderQuery = injectQuery(() => this.#options.folder(this.folderId()))
	readonly #updateFolder = injectMutation(() => this.#options.updateFolder())
	readonly #updateAsset = injectMutation(() => this.#options.updateAsset())
	protected readonly busy = computed(() => this.#updateFolder.isPending() || this.#updateAsset.isPending())
	protected readonly getMediaErrorMessage = getMediaErrorMessage
	protected readonly errorMessage = computed(() => {
		const error = this.#updateFolder.error() ?? this.#updateAsset.error()

		return error ? getMediaErrorMessage(error) : ''
	})
	protected readonly hasChoices = computed(() => this.mediaQuery.data()?.items.some(item => item.type === 'folder'))
	protected readonly forbidden = computed(
		() =>
			this.context.item.type === 'folder' &&
			(this.folderId() === this.context.item.id ||
				!!this.folderQuery.data()?.ancestors.some(item => item.id === this.context.item.id)),
	)
	protected navigateToFolder(id: string | null): void {
		this.folderId.set(id)
		this.offset.set(0)
		this.search.set('')
	}
	protected confirm(): void {
		if (
			this.busy() ||
			this.forbidden() ||
			this.mediaQuery.isFetching() ||
			this.mediaQuery.isError() ||
			(this.folderId() !== null && !this.folderQuery.isSuccess())
		)
			return

		const item = this.context.item
		const callbacks = { onSuccess: (updated: MediaItem) => this.dialog.close(updated) }

		if (item.type === 'folder')
			this.#updateFolder.mutate({ id: item.id, patch: { parentId: this.folderId() } }, callbacks)
		else this.#updateAsset.mutate({ id: item.id, patch: { folderId: this.folderId() } }, callbacks)
	}
}
