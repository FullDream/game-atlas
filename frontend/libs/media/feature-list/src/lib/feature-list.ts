import { CdkMonitorFocus } from '@angular/cdk/a11y'
import { CdkDrag, CdkDropList, type CdkDragDrop } from '@angular/cdk/drag-drop'
import { BreakpointObserver } from '@angular/cdk/layout'
import { Component, computed, DestroyRef, effect, inject, signal, viewChildren } from '@angular/core'
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop'
import { FormsModule } from '@angular/forms'
import { ActivatedRoute, Router, RouterLink, RouterOutlet } from '@angular/router'
import {
	mediaParent,
	MediaQueryOptions,
	type MediaFolder,
	type MediaItem,
	type MediaUploadMutation,
} from '@game-atlas/media/data-access'
import { Button, ErrorState, PageHeaderPortal, PendingChanges, type WorkspaceBreadcrumb } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matArrowUpwardRound,
	matCloudOffRound,
	matCreateNewFolderRound,
	matFolderRound,
	matRefreshRound,
	matUploadRound,
} from '@ng-icons/material-symbols/round'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmNumberedPaginationQueryParams } from '@spartan-ng/helm/pagination'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { injectMutation, injectMutationState, injectQuery } from '@tanstack/angular-query-experimental'
import { linkedQueryParam } from 'ngxtension/linked-query-param'
import { filter, map, take } from 'rxjs'
import { ConfirmationDialog } from './confirmation-dialog/confirmation-dialog'
import { FolderEditorDialog } from './folder-editor-dialog/folder-editor-dialog'
import { getMediaErrorMessage } from './get-media-error-message'
import { MediaCard } from './media-card/media-card'
import { MediaPickerDialog } from './media-picker-dialog/media-picker-dialog'
import { MediaUploadCard } from './media-upload-card/media-upload-card'
import {
	folderIdQueryParamOptions,
	limitQueryParamOptions,
	offsetQueryParamOptions,
	searchQueryParamOptions,
} from './feature-list-query-params'

@Component({
	selector: 'media-feature-list',
	imports: [
		CdkDrag,
		CdkMonitorFocus,
		CdkDropList,
		HlmNumberedPaginationQueryParams,
		Button,
		ErrorState,
		HlmEmptyImports,
		HlmInput,
		NgIcon,
		RouterOutlet,
		FormsModule,
		MediaCard,
		PageHeaderPortal,
		RouterLink,
		HlmTooltip,
		MediaUploadCard,
	],
	templateUrl: './feature-list.html',
	styleUrl: './feature-list.css',
	providers: [
		provideIcons({
			matFolderRound,
			matUploadRound,
			matCreateNewFolderRound,
			matArrowUpwardRound,
			matCloudOffRound,
			matRefreshRound,
		}),
	],
	host: { class: 'flex min-h-0 flex-1 flex-col overflow-hidden' },
})
export class FeatureList {
	readonly #activatedRoute = inject(ActivatedRoute)
	readonly #router = inject(Router)
	readonly #mediaQueryOptions = inject(MediaQueryOptions)
	readonly #pendingChanges = inject(PendingChanges)
	readonly #breakpointObserver = inject(BreakpointObserver)
	readonly #dialogService = inject(BrnDialogService)
	readonly #destroyRef = inject(DestroyRef)

	protected readonly folderId = linkedQueryParam('folderId', folderIdQueryParamOptions)
	protected readonly limit = linkedQueryParam('limit', limitQueryParamOptions)
	protected readonly offset = linkedQueryParam('offset', offsetQueryParamOptions)
	protected readonly search = linkedQueryParam('search', searchQueryParamOptions)

	readonly #listParams = computed(() => ({
		folderId: this.folderId() ?? undefined,
		offset: this.offset(),
		limit: this.limit(),
		search: this.search().trim(),
	}))

	protected readonly mediaQuery = injectQuery(() => this.#mediaQueryOptions.list(this.#listParams()))
	protected readonly folderQuery = injectQuery(() => this.#mediaQueryOptions.folder(this.folderId()))
	protected readonly uploadMutations = injectMutationState(() => this.#mediaQueryOptions.uploadState(this.folderId()))

	readonly #uploadMediaMutation = injectMutation(() => this.#mediaQueryOptions.upload())
	readonly #removeMediaMutation = injectMutation(() => this.#mediaQueryOptions.remove())
	readonly #updateFolderMutation = injectMutation(() => this.#mediaQueryOptions.updateFolder())
	readonly #updateAssetMutation = injectMutation(() => this.#mediaQueryOptions.updateAsset())

	protected readonly getMediaErrorMessage = getMediaErrorMessage
	protected readonly uploadProgress = this.#mediaQueryOptions.uploadProgress

	protected readonly acceptsDrop = (drag: CdkDrag<string>, list: CdkDropList<string | null>): boolean =>
		!list.disabled && this.canDrop(list.data, drag.data)

	#opener: HTMLElement | null = null

	protected readonly showActionLabels = toSignal(
		this.#breakpointObserver.observe('(min-width: 640px)').pipe(map(result => result.matches)),
		{ initialValue: false },
	)

	protected readonly busy = computed(
		() =>
			this.#removeMediaMutation.isPending() ||
			this.#updateFolderMutation.isPending() ||
			this.#updateAssetMutation.isPending(),
	)

	protected readonly loadError = computed(() => this.mediaQuery.error() ?? this.folderQuery.error())

	protected readonly currentFolder = computed(() => {
		const folderId = this.folderId()
		const currentFolder = this.folderQuery.data()?.folder
		const cachedFolder = this.#mediaQueryOptions.getCachedFolder(folderId)

		return cachedFolder ?? (currentFolder?.id === folderId ? currentFolder : undefined)
	})

	protected readonly skeletonItemCount = computed(() => {
		const count = this.currentFolder()?.itemsCount

		return count !== undefined && count <= this.limit() ? count : null
	})

	protected readonly skeletonItems = computed(() =>
		Array.from({ length: this.skeletonItemCount() ?? 24 }, (value, index) => index),
	)

	protected readonly dragged = signal<string | null>(null)
	protected readonly dropLists = viewChildren(CdkDropList)
	protected readonly hoveredFolder = signal<string | null>(null)

	protected readonly breadcrumbs = computed<WorkspaceBreadcrumb[]>(() => {
		const data = this.folderQuery.data()
		const parents = (data?.ancestors ?? []).map(parent => this.#createBreadcrumb(parent.name, parent.id))

		return [
			this.#createBreadcrumb('Media', null),
			...parents,
			...(data?.folder ? [this.#createBreadcrumb(data.folder.name, data.folder.id)] : []),
		]
	})

	readonly #lastOffset = computed(
		() => Math.floor(Math.max(0, (this.mediaQuery.data()?.total ?? 0) - 1) / this.limit()) * this.limit(),
	)

	constructor() {
		effect(() => {
			if (
				!this.busy() &&
				this.mediaQuery.isSuccess() &&
				!this.mediaQuery.isPlaceholderData() &&
				this.offset() > 0 &&
				this.offset() >= this.mediaQuery.data().total
			) {
				void this.#router.navigate([], {
					relativeTo: this.#activatedRoute,
					queryParams: { offset: this.#lastOffset() === 0 ? null : this.#lastOffset() },
					queryParamsHandling: 'merge',
					replaceUrl: true,
				})
			}
		})
	}

	protected retry(): void {
		if (this.mediaQuery.isError()) void this.mediaQuery.refetch()

		if (this.folderId() && this.folderQuery.isError()) void this.folderQuery.refetch()
	}

	protected rememberOpener(event: Event): void {
		this.#opener = event.currentTarget as HTMLElement
	}

	protected restoreFocus(): void {
		this.#opener?.focus()
	}

	protected uploadFile(event: Event): void {
		const input = event.target as HTMLInputElement
		const file = input.files?.[0]

		if (file) {
			this.#uploadMediaMutation.mutate({
				key: crypto.randomUUID(),
				file,
				folderId: this.folderId(),
				controller: new AbortController(),
			})

			input.value = ''
		}
	}

	protected retryUpload(upload: MediaUploadMutation): void {
		this.#uploadMediaMutation.mutate({ ...upload.variables, controller: new AbortController() })
	}

	protected openFolderEditor(folder: MediaFolder | null = null): void {
		this.#dialogService.open(
			FolderEditorDialog,
			undefined,
			{ folder, parentId: this.folderId() },
			{ ariaLabel: folder ? 'Rename folder' : 'New folder' },
		)
	}

	protected moveItem(item: MediaItem): void {
		this.#whenLeavingAllowed(() => {
			this.#dismissDetails()

			this.#dialogService.open<{ mode: 'move'; item: MediaItem; folderId: string | null }, MediaItem>(
				MediaPickerDialog,
				undefined,
				{ mode: 'move', item, folderId: mediaParent(item) },
				{ ariaLabel: 'Move item' },
			)
		})
	}

	protected removeItem(item: MediaItem): void {
		this.#whenLeavingAllowed(() => {
			const dialog = this.#dialogService.open(
				ConfirmationDialog,
				undefined,
				{
					title: `Delete ${item.type === 'folder' ? 'folder' : 'file'}?`,
					description: `“${item.name}” will be permanently deleted. Used files and nonempty folders cannot be deleted.`,
					confirm: 'Delete',
					cancel: 'Cancel',
					destructive: true,
				},
				{ ariaLabel: 'Confirm deletion', role: 'alertdialog' },
			)

			dialog.closed$
				.pipe(
					take(1),
					filter(result => result === true),
					takeUntilDestroyed(this.#destroyRef),
				)
				.subscribe(() => {
					this.#removeMediaMutation.mutate(item, {
						onError: error => toast.error(getMediaErrorMessage(error)),
						onSuccess: () => {
							if (this.#activatedRoute.firstChild?.snapshot.paramMap.get('id') === item.id) {
								this.#dismissDetails()
							}
						},
					})
				})
		})
	}

	protected endDrag(): void {
		this.dragged.set(null)
		this.hoveredFolder.set(null)
	}

	protected canDrop(folderId: string | null, itemId = this.dragged()): boolean {
		const item = this.mediaQuery.data()?.items.find(item => item.id === itemId)

		return !!item && !this.busy() && item.id !== folderId && mediaParent(item) !== folderId
	}

	protected dropped<T extends string | null>(event: CdkDragDrop<T, unknown, string>): void {
		if (event.isPointerOverContainer) this.#dropToFolder(event.container.data, event.item.data)
	}

	#dismissDetails(): void {
		void this.#router.navigate([{ outlets: { details: null } }], {
			relativeTo: this.#activatedRoute,
			queryParamsHandling: 'preserve',
			replaceUrl: true,
		})
	}

	#dropToFolder(folderId: string | null, itemId: string): void {
		const item = this.mediaQuery.data()?.items.find(item => item.id === itemId)

		if (!item || !this.canDrop(folderId, itemId)) return

		this.#whenLeavingAllowed(() => {
			this.#dismissDetails()

			const callbacks = { onError: (error: unknown) => toast.error(getMediaErrorMessage(error)) }

			if (item.type === 'folder') {
				this.#updateFolderMutation.mutate({ id: item.id, patch: { parentId: folderId } }, callbacks)
			} else {
				this.#updateAssetMutation.mutate({ id: item.id, patch: { folderId } }, callbacks)
			}
		})
	}

	#whenLeavingAllowed(action: () => void): void {
		this.#pendingChanges
			.canLeave()
			.pipe(take(1), filter(Boolean), takeUntilDestroyed(this.#destroyRef))
			.subscribe(action)
	}

	#createBreadcrumb(label: string, id: string | null): WorkspaceBreadcrumb {
		return {
			label,
			url: '/media',
			queryParams: { folderId: id, limit: this.limit() === 24 ? null : this.limit() },
			drop: { accepts: itemId => this.canDrop(id, itemId), perform: itemId => this.#dropToFolder(id, itemId) },
		}
	}
}
