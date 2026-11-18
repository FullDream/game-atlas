import { Component, computed, inject, input, output, signal } from '@angular/core'
import { RouterLink } from '@angular/router'
import { FormsModule } from '@angular/forms'
import {
	MapClientDb,
	type MapConfig,
	type PublicMapDetails,
	type GroupConfig,
	type ImageAsset,
	type PublicCategory,
	type PublicCategoryGroup,
} from '@game-atlas/maps/data-access'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matHomeRound,
	matSearchRound,
	matVisibilityRound,
	matVisibilityOffRound,
	matChevronLeftRound,
	matKeyboardArrowDownRound,
	matMapRound,
} from '@ng-icons/material-symbols/round'
import { Button, MediaThumbnail } from '@game-atlas/shared/ui'
import { HlmAccordionImports } from '@spartan-ng/helm/accordion'
import { HlmBreadcrumbImports } from '@spartan-ng/helm/breadcrumb'
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { HlmSwitch } from '@spartan-ng/helm/switch'
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu'

export type PublicMapChoice = {
	id: string
	name: string
	slug: string
	preview: ImageAsset | null
}

@Component({
	selector: 'maps-category-filter-panel',
	imports: [
		Button,
		MediaThumbnail,
		HlmAccordionImports,
		HlmBreadcrumbImports,
		HlmInputGroupImports,
		HlmEmptyImports,
		HlmTooltip,
		RouterLink,
		FormsModule,
		NgIcon,
		HlmSwitch,
		HlmDropdownMenuImports,
	],
	providers: [
		provideIcons({
			matHomeRound,
			matSearchRound,
			matVisibilityRound,
			matVisibilityOffRound,
			matChevronLeftRound,
			matKeyboardArrowDownRound,
			matMapRound,
		}),
	],
	templateUrl: './category-filter-panel.html',
	host: {
		class: 'flex h-full w-full flex-col overflow-hidden',
	},
})
export class CategoryFilterPanel {
	readonly mobile = input(false)
	readonly gameSlug = input.required<string>()
	readonly gameName = input<string>('Game')
	readonly gameCover = input<string | null>(null)
	readonly gameBanner = input<string | null>(null)
	readonly maps = input<readonly PublicMapChoice[]>([])
	readonly mapSlug = input.required<string>()
	readonly mapName = input<string>('Map')
	readonly data = input.required<PublicMapDetails>()
	readonly visibleCategoryIds = input.required<ReadonlySet<string>>()
	readonly groupConfigs = input.required<ReadonlyMap<string, GroupConfig>>()
	readonly config = input<MapConfig>()
	readonly closed = output<void>()

	readonly #db = inject(MapClientDb)
	readonly groups = computed(() => this.data().categoryGroups)

	readonly completedCountByCategoryId = this.#db.liveQuerySignal({
		params: () => this.data().id,
		querier: id =>
			this.#db.completedMarkers
				.where('mapId')
				.equals(id)
				.toArray(records => {
					const counts = new Map<string, number>()

					for (const record of records)
						counts.set(record.categoryId, (counts.get(record.categoryId) ?? 0) + 1)

					return counts
				}),
		initialValue: new Map<string, number>(),
	})
	protected readonly searchQuery = signal('')

	readonly filteredGroups = computed(() => {
		const query = this.searchQuery().trim().toLowerCase()
		const allGroups = this.groups()

		if (!query) return allGroups

		return allGroups
			.map(group => {
				const groupMatches = group.name.toLowerCase().includes(query)
				const matchedCategories = group.categories.filter(cat => cat.name.toLowerCase().includes(query))

				if (groupMatches) return group

				if (matchedCategories.length > 0) {
					return {
						...group,
						categories: matchedCategories,
					}
				}

				return null
			})
			.filter((group): group is PublicCategoryGroup => group !== null)
	})

	protected readonly currentMap = computed(() => this.maps().find(map => map.slug === this.mapSlug()))

	isTargetHidden(id: string): boolean {
		return this.groupConfigs().get(id)?.visible === false
	}

	isCategoryHidden(category: PublicCategory, parentGroupId: string): boolean {
		return (
			this.isTargetHidden(parentGroupId) ||
			(!!this.config()?.hiddenCategories && !this.visibleCategoryIds().has(category.id))
		)
	}

	getCategoryCompletedCount(categoryId: string): number {
		return this.completedCountByCategoryId().get(categoryId) ?? 0
	}

	getGroupCompletedCount(group: PublicCategoryGroup): number {
		let total = 0

		for (const cat of group.categories) {
			total += this.getCategoryCompletedCount(cat.id)
		}

		return total
	}

	async setGroup(id: string, changes: Partial<Pick<GroupConfig, 'expanded' | 'visible'>>): Promise<void> {
		const { id: mapId, gameId } = this.data()
		const { groupConfigs } = this.#db

		await this.#db.transaction('rw', groupConfigs, async () => {
			if (!(await groupConfigs.update(id, changes))) {
				await groupConfigs.add({ id, mapId, gameId, expanded: true, visible: true, ...changes })
			}
		})
	}

	async setConfig(changes: Partial<Pick<MapConfig, 'hiddenCategories' | 'hiddenCompleted'>>): Promise<void> {
		const { id, gameId } = this.data()
		const { mapConfig } = this.#db

		await this.#db.transaction('rw', mapConfig, async () => {
			if (!(await mapConfig.update(id, changes))) {
				await mapConfig.add({ id, gameId, hiddenCategories: false, hiddenCompleted: false, ...changes })
			}
		})
	}

	async toggleCategory(category: PublicCategory): Promise<void> {
		const { id: mapId, gameId, categoryGroups } = this.data()
		const { mapConfig, visibleCategories } = this.#db

		await this.#db.transaction('rw', mapConfig, visibleCategories, async () => {
			const config = await mapConfig.get(mapId)

			if (!config?.hiddenCategories) {
				await visibleCategories.bulkPut(
					categoryGroups.flatMap(group =>
						group.categories.map(item => ({ id: item.id, groupId: group.id, mapId, gameId })),
					),
				)

				await this.setConfig({ hiddenCategories: true })
			}

			if (await visibleCategories.get(category.id)) await visibleCategories.delete(category.id)
			else await visibleCategories.put({ id: category.id, groupId: category.categoryGroupId, mapId, gameId })
		})
	}

	async setAllVisible(visible: boolean): Promise<void> {
		const { id: mapId, gameId, categoryGroups } = this.data()
		const { mapConfig, groupConfigs, visibleCategories } = this.#db

		await this.#db.transaction('rw', mapConfig, groupConfigs, visibleCategories, async () => {
			await this.setConfig({ hiddenCategories: !visible })
			await visibleCategories.where('mapId').equals(mapId).delete()

			const configs = await groupConfigs.bulkGet(categoryGroups.map(group => group.id))

			await groupConfigs.bulkPut(
				categoryGroups.map((group, index) => ({
					id: group.id,
					gameId,
					mapId,
					expanded: configs[index]?.expanded ?? true,
					visible,
				})),
			)
		})
	}
}
