export const getGroupsQueryKey = (gameId: string, mapId: string) => ['categories', { gameId, mapId }] as const

export type GroupsQueryKey = ReturnType<typeof getGroupsQueryKey>
