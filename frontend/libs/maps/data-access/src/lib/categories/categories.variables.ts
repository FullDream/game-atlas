import type {
	CreateCategoryRequest,
	UpdateCategoryRequest,
	CreateCategoryGroupRequest,
	UpdateCategoryGroupRequest,
} from '../generated/models'

type MapCategoryVariables = { gameId: string; mapId: string }
type BaseCategoryMutationVariables = MapCategoryVariables & { groupId: string }

export type CreateCategoryVariables = BaseCategoryMutationVariables & CreateCategoryRequest
export type UpdateCategoryVariables = { id: string } & BaseCategoryMutationVariables & UpdateCategoryRequest
export type DeleteCategoryVariables = { id: string } & BaseCategoryMutationVariables

export type CreateCategoryGroupVariables = MapCategoryVariables & CreateCategoryGroupRequest
export type UpdateCategoryGroupVariables = { id: string } & MapCategoryVariables & UpdateCategoryGroupRequest
export type DeleteCategoryGroupVariables = { id: string } & MapCategoryVariables
