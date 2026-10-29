export const mapQueryKeys = {
	list: (gameId: string) => ['maps', gameId, 'list'] as const,
	detail: (gameId: string, mapId: string) => ['maps', gameId, mapId, 'detail'] as const,
}
