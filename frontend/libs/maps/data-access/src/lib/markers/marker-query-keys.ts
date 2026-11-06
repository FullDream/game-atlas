export const getMarkersQueryKey = (gameId: string, mapId: string) => ['markers', { gameId, mapId }] as const

export type MarkersQueryKey = ReturnType<typeof getMarkersQueryKey>
