import { HttpErrorResponse } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import {
	type CreateMutationOptions,
	type CreateQueryOptions,
	mutationOptions,
	queryOptions,
} from '@tanstack/angular-query-experimental'
import { reorder, toPromise } from '@game-atlas/shared/util'
import { CategoriesService } from '../generated'
import type { Category, CategoryGroup } from '../generated/models'
import { type GroupsQueryKey, getGroupsQueryKey } from './category-query-keys'
import { getMarkersQueryKey } from '../markers/marker-query-keys'
import type {
	CreateCategoryVariables,
	UpdateCategoryVariables,
	DeleteCategoryVariables,
	CreateCategoryGroupVariables,
	UpdateCategoryGroupVariables,
	DeleteCategoryGroupVariables,
} from './categories.variables'

type PreviousQueryCache = { previousGroups: CategoryGroup[] | undefined }

@Service()
export class CategoriesQueryOptions {
	readonly #service = inject(CategoriesService)

	getGroups(
		gameId: string,
		mapId: string,
	): CreateQueryOptions<CategoryGroup[], HttpErrorResponse, CategoryGroup[], GroupsQueryKey> {
		return queryOptions({
			queryKey: getGroupsQueryKey(gameId, mapId),
			queryFn: ({ signal }) => toPromise(this.#service.listCategoryGroups(gameId, mapId), signal),
			staleTime: 30_000,
			enabled: Boolean(gameId && mapId),
		})
	}

	createGroup(): CreateMutationOptions<CategoryGroup, HttpErrorResponse, CreateCategoryGroupVariables> {
		return mutationOptions({
			mutationKey: ['categories', 'createGroup'],
			mutationFn: ({ gameId, mapId, ...payload }) =>
				toPromise(this.#service.createCategoryGroup(gameId, mapId, payload)),
			onSuccess: (group, { gameId, mapId }, snapshot, { client }) =>
				client.setQueryData<CategoryGroup[]>(getGroupsQueryKey(gameId, mapId), groups =>
					groups ? [...groups, group] : undefined,
				),
		})
	}

	updateGroup(): CreateMutationOptions<
		CategoryGroup,
		HttpErrorResponse,
		UpdateCategoryGroupVariables,
		PreviousQueryCache
	> {
		return mutationOptions({
			mutationKey: ['categories', 'updateGroup'],
			mutationFn: ({ gameId, mapId, id, ...payload }) =>
				toPromise(this.#service.updateCategoryGroup(gameId, mapId, id, payload)),
			onMutate: async (variables, context) => {
				const queryKey = getGroupsQueryKey(variables.gameId, variables.mapId)

				await context.client.cancelQueries({ queryKey })

				const previousGroups = context.client.getQueryData<CategoryGroup[]>(queryKey)

				context.client.setQueryData<CategoryGroup[]>(queryKey, oldGroups => {
					if (!oldGroups) return

					const groups = oldGroups?.map(group =>
						group.id === variables.id
							? {
									...group,
									name: variables.name ?? group.name,
									color: variables.color ?? group.color,
									icon: variables.iconAssetId === null ? null : group.icon,
								}
							: group,
					)

					if (variables.beforeId === undefined || variables.beforeId === variables.id) return groups

					return reorder(groups, variables.id, variables.beforeId)
				})

				return { previousGroups }
			},

			onError: (error, result, onMutateResult, context) =>
				context.client.setQueryData<CategoryGroup[]>(
					getGroupsQueryKey(result.gameId, result.mapId),
					onMutateResult?.previousGroups,
				),

			onSuccess: (data, variables, onMutateResult, context) =>
				context.client.setQueryData<CategoryGroup[]>(
					getGroupsQueryKey(variables.gameId, variables.mapId),
					groups => groups?.map(item => (item.id === data.id ? data : item)),
				),
		})
	}

	deleteGroup(): CreateMutationOptions<void, HttpErrorResponse, DeleteCategoryGroupVariables, PreviousQueryCache> {
		return mutationOptions({
			mutationKey: ['categories', 'deleteGroup'],
			mutationFn: ({ gameId, mapId, id }) => toPromise(this.#service.deleteCategoryGroup(gameId, mapId, id)),

			onMutate: async (variables, context) => {
				const queryKey = getGroupsQueryKey(variables.gameId, variables.mapId)

				await context.client.cancelQueries({ queryKey })

				const previousGroups = context.client.getQueryData<CategoryGroup[]>(queryKey)

				context.client.setQueryData<CategoryGroup[]>(queryKey, old =>
					old?.filter(group => group.id !== variables.id),
				)

				return { previousGroups }
			},

			onSuccess: async (data, { gameId, mapId }, snapshot, { client }) => {
				await client.invalidateQueries({ queryKey: getMarkersQueryKey(gameId, mapId), exact: true })
			},

			onError: (error, result, onMutateResult, context) =>
				context.client.setQueryData<CategoryGroup[]>(
					getGroupsQueryKey(result.gameId, result.mapId),
					onMutateResult?.previousGroups,
				),
		})
	}

	create(): CreateMutationOptions<Category, HttpErrorResponse, CreateCategoryVariables> {
		return mutationOptions({
			mutationKey: ['categories', 'create'],
			mutationFn: ({ gameId, mapId, groupId, ...payload }) =>
				toPromise(this.#service.createCategory(gameId, mapId, groupId, payload)),
			onSuccess: (category, { gameId, mapId }, snapshot, { client }) =>
				client.setQueryData<CategoryGroup[]>(getGroupsQueryKey(gameId, mapId), groups =>
					groups?.map(group =>
						group.id === category.categoryGroupId
							? { ...group, categories: [...group.categories, category] }
							: group,
					),
				),
		})
	}

	update(): CreateMutationOptions<Category, HttpErrorResponse, UpdateCategoryVariables, PreviousQueryCache> {
		return mutationOptions({
			mutationKey: ['categories', 'update'],
			mutationFn: ({ gameId, mapId, groupId, id, ...payload }) =>
				toPromise(this.#service.updateCategory(gameId, mapId, groupId, id, payload)),
			onMutate: async (variables, context) => {
				const queryKey = getGroupsQueryKey(variables.gameId, variables.mapId)

				await context.client.cancelQueries({ queryKey })

				const previousGroups = context.client.getQueryData<CategoryGroup[]>(queryKey)

				context.client.setQueryData<CategoryGroup[]>(queryKey, groups => {
					const category = groups?.flatMap(group => group.categories).find(item => item.id === variables.id)

					if (!category || !groups) return groups

					const categoryGroupId = variables.categoryGroupId ?? category.categoryGroupId

					if (!groups.some(group => group.id === categoryGroupId)) return groups

					const updatedCategory = {
						...category,
						categoryGroupId,
						name: variables.name ?? category.name,
						color: variables.color === undefined ? category.color : variables.color,
						icon: variables.iconAssetId === null ? null : category.icon,
					}

					return groups.map(group => {
						if (group.id !== categoryGroupId) {
							return { ...group, categories: group.categories.filter(item => item.id !== category.id) }
						}

						const categories = group.categories.map(item =>
							item.id === category.id ? updatedCategory : item,
						)

						if (category.categoryGroupId !== categoryGroupId) categories.push(updatedCategory)

						const beforeId = variables.beforeId

						return {
							...group,
							categories:
								beforeId === undefined ? categories : reorder(categories, category.id, beforeId),
						}
					})
				})

				return { previousGroups }
			},

			onError: (error, result, onMutateResult, context) =>
				context.client.setQueryData<CategoryGroup[]>(
					getGroupsQueryKey(result.gameId, result.mapId),
					onMutateResult?.previousGroups,
				),

			onSuccess: (data, variables, onMutateResult, context) =>
				context.client.setQueryData<CategoryGroup[]>(
					getGroupsQueryKey(variables.gameId, variables.mapId),
					groups =>
						groups?.map(group => ({
							...group,
							categories: group.categories.map(item => (item.id === data.id ? data : item)),
						})),
				),
		})
	}

	delete(): CreateMutationOptions<void, HttpErrorResponse, DeleteCategoryVariables, PreviousQueryCache> {
		return mutationOptions({
			mutationKey: ['categories', 'delete'],
			mutationFn: ({ gameId, mapId, groupId, id }) =>
				toPromise(this.#service.deleteCategory(gameId, mapId, groupId, id)),
			onMutate: async (variables, context) => {
				const queryKey = getGroupsQueryKey(variables.gameId, variables.mapId)

				await context.client.cancelQueries({ queryKey })

				const previousGroups = context.client.getQueryData<CategoryGroup[]>(queryKey)

				context.client.setQueryData<CategoryGroup[]>(queryKey, old =>
					old?.map(group => ({
						...group,
						categories: group.categories.filter(category => variables.id !== category.id),
					})),
				)

				return { previousGroups }
			},

			onSuccess: async (data, { gameId, mapId }, snapshot, { client }) => {
				await client.invalidateQueries({ queryKey: getMarkersQueryKey(gameId, mapId), exact: true })
			},

			onError: (error, result, onMutateResult, context) =>
				context.client.setQueryData<CategoryGroup[]>(
					getGroupsQueryKey(result.gameId, result.mapId),
					onMutateResult?.previousGroups,
				),
		})
	}
}
