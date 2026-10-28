import { Component, computed, inject, signal } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { MediaQueryOptions, type ImageAsset, type MediaItem } from '@game-atlas/media/data-access'
import { Button } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matFolderRound } from '@ng-icons/material-symbols/round'
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmInput } from '@spartan-ng/helm/input'
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental'
import { getMediaErrorMessage } from '../get-media-error-message'
import { MediaThumbnail } from '../media-thumbnail/media-thumbnail'

type MediaPickerContext =
	{ mode: 'image'; folderId: string | null } | { mode: 'move'; item: MediaItem; folderId: string | null }

@Component({
	selector: 'media-picker-dialog',
	imports: [Button, HlmInput, NgIcon, MediaThumbnail, FormsModule],
	templateUrl: './media-picker-dialog.html',
	providers: [provideIcons({ matFolderRound })],
	host: {
		'[attr.data-state]': 'dialog.state()',
		class: 'flex max-h-[90dvh] w-[min(48rem,calc(100vw-2rem))] flex-col rounded-xl border bg-background p-5 text-foreground shadow-xl',
	},
})
export class MediaPickerDialog {
	protected readonly context = injectBrnDialogContext<MediaPickerContext>()
	protected readonly dialog = inject(BrnDialogRef)
	readonly #mediaQueryOptions = inject(MediaQueryOptions)

	protected readonly folderId = signal(this.context.folderId)
	protected readonly offset = signal(0)
	protected readonly search = signal('')

	readonly #listParams = computed(() => ({
		folderId: this.folderId() ?? undefined,
		offset: this.offset(),
		limit: 24,
		search: this.search().trim(),
	}))

	protected readonly mediaQuery = injectQuery(() => this.#mediaQueryOptions.list(this.#listParams()))
	protected readonly folderQuery = injectQuery(() => this.#mediaQueryOptions.folder(this.folderId()))

	readonly #updateFolderMutation = injectMutation(() => this.#mediaQueryOptions.updateFolder())
	readonly #updateAssetMutation = injectMutation(() => this.#mediaQueryOptions.updateAsset())

	protected readonly getMediaErrorMessage = getMediaErrorMessage

	protected readonly selectedImage = signal<ImageAsset | null>(null)

	protected readonly busy = computed(
		() => this.#updateFolderMutation.isPending() || this.#updateAssetMutation.isPending(),
	)

	protected readonly errorMessage = computed(() => {
		const error = this.#updateFolderMutation.error() ?? this.#updateAssetMutation.error()

		return error ? getMediaErrorMessage(error) : ''
	})

	protected readonly hasChoices = computed(() =>
		this.mediaQuery
			.data()
			?.items.some(item => item.type === 'folder' || (this.context.mode === 'image' && item.type === 'image')),
	)

	protected readonly forbidden = computed(() => {
		if (this.context.mode !== 'move') return false

		const item = this.context.item
		const data = this.folderQuery.data()

		return (
			item.type === 'folder' &&
			(data?.folder?.id === item.id || data?.ancestors.some(parent => parent.id === item.id))
		)
	})

	protected navigateToFolder(id: string | null): void {
		this.folderId.set(id)
		this.offset.set(0)
		this.search.set('')
		this.selectedImage.set(null)
	}

	protected confirm(): void {
		if (this.context.mode === 'image') {
			this.dialog.close(this.selectedImage())

			return
		}

		const item = this.context.item

		if (this.forbidden() || this.busy()) {
			return
		}

		const callbacks = {
			onSuccess: (updated: MediaItem) => this.dialog.close(updated),
			onError: (error: unknown) => toast.error(getMediaErrorMessage(error)),
		}

		if (item.type === 'folder') {
			this.#updateFolderMutation.mutate({ id: item.id, patch: { parentId: this.folderId() } }, callbacks)
		} else {
			this.#updateAssetMutation.mutate({ id: item.id, patch: { folderId: this.folderId() } }, callbacks)
		}
	}
}
