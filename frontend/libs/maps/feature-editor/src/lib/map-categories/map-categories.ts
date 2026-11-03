import { CdkScrollable } from '@angular/cdk/scrolling'
import { HlmAccordionImports } from '@spartan-ng/helm/accordion'
import { ChangeDetectorRef, Component, computed, DestroyRef, inject, input, linkedSignal, signal } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { Tree, TreeItem, TreeItemGroup } from '@angular/aria/tree'
import {
	CdkDrag,
	CdkDragHandle,
	CdkDropList,
	moveItemInArray,
	transferArrayItem,
	type CdkDragDrop,
} from '@angular/cdk/drag-drop'
import {
	CategoriesQueryOptions,
	getCategoryErrorMessage,
	type Category,
	type CategoryGroup,
} from '@game-atlas/maps/data-access'
import { Button, ConfirmationDialog, ErrorState, MediaThumbnail } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matAddRound,
	matEditRound,
	matDeleteRound,
	matArrowUpwardRound,
	matArrowDownwardRound,
	matDriveFileMoveRound,
	matFolderRound,
	matLocationOnRound,
	matMoreHorizRound,
	matDragIndicatorRound,
	matChevronRightRound,
	matKeyboardArrowDownRound,
} from '@ng-icons/material-symbols/round'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmButtonGroup } from '@spartan-ng/helm/button-group'
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { injectQuery, injectMutation, injectIsMutating } from '@tanstack/angular-query-experimental'
import { take } from 'rxjs'
import { CategoryEditorDialog } from './category-editor-dialog/category-editor-dialog'

type DragItem = { kind: 'group' | 'category'; id: string; groupId: string }
type DropTarget = { kind: string; groupId: string }
type MoveCallbacks = { onError: (error: unknown) => void }

@Component({
	selector: 'maps-categories',
	imports: [
		CdkScrollable,
		HlmAccordionImports,
		Button,
		NgIcon,
		HlmButtonGroup,
		ErrorState,
		MediaThumbnail,
		HlmDropdownMenuImports,
		HlmEmptyImports,
		HlmSkeleton,
		Tree,
		TreeItem,
		TreeItemGroup,
		CdkDrag,
		CdkDragHandle,
		CdkDropList,
	],
	templateUrl: './map-categories.html',
	styleUrl: './map-categories.css',
	providers: [
		provideIcons({
			matEditRound,
			matDeleteRound,
			matArrowUpwardRound,
			matArrowDownwardRound,
			matDriveFileMoveRound,
			matFolderRound,
			matLocationOnRound,
			matAddRound,
			matMoreHorizRound,
			matDragIndicatorRound,
			matChevronRightRound,
			matKeyboardArrowDownRound,
		}),
	],
	host: {
		class: 'flex min-h-14 basis-14 shrink-0 flex-col transition-[flex-grow] duration-200 ease-out motion-reduce:transition-none',
		'[style.flex-grow]': 'sectionOpen() ? 1 : 0',
	},
})
export class MapCategories {
	readonly gameId = input.required<string>()
	readonly mapId = input.required<string>()

	readonly #options = inject(CategoriesQueryOptions)
	readonly #dialogs = inject(BrnDialogService)
	readonly #destroy = inject(DestroyRef)
	readonly #changeDetector = inject(ChangeDetectorRef)

	protected readonly query = injectQuery(() => this.#options.getGroups(this.gameId(), this.mapId()))

	readonly #pending = injectIsMutating({
		mutationKey: ['categories'],
		predicate: mutation => {
			const variables = mutation.state.variables as { gameId?: string; mapId?: string } | undefined

			return variables?.gameId === this.gameId() && variables.mapId === this.mapId()
		},
	})

	readonly #update = injectMutation(() => this.#options.update())
	readonly #updateGroup = injectMutation(() => this.#options.updateGroup())
	readonly #delete = injectMutation(() => this.#options.delete())
	readonly #deleteGroup = injectMutation(() => this.#options.deleteGroup())

	protected readonly groups = linkedSignal(() => this.query.data() ?? [])
	protected readonly busy = computed(() => this.#pending() > 0)
	protected readonly dropIds = computed(() =>
		this.groups().flatMap(group => ['category-list-' + group.id, 'category-header-' + group.id]),
	)
	protected readonly collapsed = signal<ReadonlySet<string>>(new Set())
	readonly sectionOpen = signal(true)
	protected readonly dragging = signal(false)
	protected readonly acceptsGroup = (drag: CdkDrag<DragItem | undefined>): boolean => drag.data?.kind === 'group'
	protected readonly acceptsCategory = (drag: CdkDrag<DragItem | undefined>): boolean =>
		drag.data?.kind === 'category'

	protected expanded(id: string): boolean {
		return !this.collapsed().has(id)
	}

	protected setExpanded(id: string, expanded: boolean): void {
		this.collapsed.update(previous => {
			const next = new Set(previous)

			if (expanded) next.delete(id)
			else next.add(id)

			return next
		})
	}

	protected edit(
		kind: 'group' | 'category',
		group: CategoryGroup | null = null,
		category: Category | null = null,
	): void {
		if (this.busy()) return

		if (kind === 'category' && !this.groups().length) {
			toast.info('Create a group before adding a category.')
			this.edit('group')

			return
		}

		this.#dialogs.open(
			CategoryEditorDialog,
			undefined,
			{ gameId: this.gameId(), mapId: this.mapId(), groups: this.groups(), kind, group, category },
			{
				ariaLabel: (category || (kind === 'group' && group) ? 'Edit ' : 'Add ') + kind,
				restoreFocus: category
					? '#category-actions-' + category.id
					: group
						? '#category-actions-' + group.id
						: '#category-add',
				disableClose: true,
				closeOnOutsidePointerEvents: false,
			},
		)
	}

	protected drop(event: CdkDragDrop<DropTarget, DropTarget, DragItem>): void {
		this.dragging.set(false)

		if (this.busy()) return

		const item = event.item.data
		const target = event.container.data

		if (item.kind === 'group' && target.kind === 'group') {
			this.moveGroup(item.id, event.currentIndex)

			return
		}

		if (item.kind !== 'category' || target.kind !== 'category') return

		const destination = this.groups().find(group => group.id === target.groupId)
		const source = this.groups().find(group => group.id === item.groupId)
		const category = source?.categories.find(category => category.id === item.id)

		if (!destination || !source || !category) return

		const index = event.container.id.startsWith('category-header-')
			? destination.categories.filter(category => category.id !== item.id).length
			: event.currentIndex

		this.moveCategory(source, category, destination, index)
	}

	protected moveGroup(id: string, index: number): void {
		const groups = [...this.groups()]
		const current = groups.findIndex(group => group.id === id)

		if (this.busy() || current < 0 || current === index || index < 0 || index >= groups.length) return

		moveItemInArray(groups, current, index)
		this.groups.set(groups)
		// TODO: Replace synchronous rendering with a CDK drop/render handoff that prevents the old-order flash.
		this.#changeDetector.detectChanges()

		this.#updateGroup.mutate(
			{ gameId: this.gameId(), mapId: this.mapId(), id, beforeId: groups[index + 1]?.id ?? null },
			{
				onError: error => toast.error(getCategoryErrorMessage(error)),
			},
		)
	}

	protected moveCategory(source: CategoryGroup, category: Category, destination: CategoryGroup, index: number): void {
		const current = source.categories.findIndex(item => item.id === category.id)
		const sameGroup = source.id === destination.id
		const sourceCategories = [...source.categories]
		const categories = sameGroup ? sourceCategories : [...destination.categories]
		const lastIndex = sameGroup ? categories.length - 1 : categories.length

		if (this.busy() || current < 0 || index < 0 || index > lastIndex || (sameGroup && current === index)) return

		if (sameGroup) moveItemInArray(categories, current, index)
		else transferArrayItem(sourceCategories, categories, current, index)

		categories[index] = { ...category, categoryGroupId: destination.id }
		this.setExpanded(destination.id, true)
		this.groups.update(groups =>
			groups.map(group => {
				if (group.id === destination.id) return { ...group, categories }

				return group.id === source.id ? { ...group, categories: sourceCategories } : group
			}),
		)

		// TODO: Replace synchronous rendering with a CDK drop/render handoff that prevents the old-order flash.
		this.#changeDetector.detectChanges()

		this.#update.mutate(
			{
				gameId: this.gameId(),
				mapId: this.mapId(),
				groupId: source.id,
				id: category.id,
				categoryGroupId: destination.id,
				beforeId: categories[index + 1]?.id ?? null,
			},
			{
				onError: error => toast.error(getCategoryErrorMessage(error)),
			},
		)
	}

	protected remove(group: CategoryGroup, category: Category | null = null): void {
		if (this.busy()) return

		const item = category ?? group

		this.#dialogs
			.open(
				ConfirmationDialog,
				undefined,
				{
					title: 'Delete “' + item.name + '”?',
					description: category
						? 'This permanently deletes the category and its markers. Library media is kept.'
						: 'This permanently deletes the group, its categories and their markers. Library media is kept.',
					confirm: 'Delete',
					cancel: 'Cancel',
					destructive: true,
				},
				{ ariaLabel: 'Confirm deletion', role: 'alertdialog', restoreFocus: '#category-add' },
			)
			.closed$.pipe(take(1), takeUntilDestroyed(this.#destroy))
			.subscribe(confirmed => {
				if (confirmed !== true || this.busy()) return

				if (category)
					this.#delete.mutate(
						{ gameId: this.gameId(), mapId: this.mapId(), groupId: group.id, id: category.id },
						{
							onError: error => toast.error(getCategoryErrorMessage(error)),
						},
					)
				else
					this.#deleteGroup.mutate(
						{ gameId: this.gameId(), mapId: this.mapId(), id: group.id },
						{
							onError: error => toast.error(getCategoryErrorMessage(error)),
						},
					)
			})
	}
}
