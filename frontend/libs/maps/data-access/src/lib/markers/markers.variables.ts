import type { CreateMarkerRequest, UpdateMarkerRequest } from '../generated/models'

export type MapMarkerVariables = { gameId: string; mapId: string }
type MarkerIdentity = { id: string }
type MarkerPlacementGroup = { requestGroup?: string }

export type CreateMarkerVariables = MapMarkerVariables & CreateMarkerRequest & MarkerPlacementGroup
export type UpdateMarkerVariables = MapMarkerVariables & MarkerIdentity & UpdateMarkerRequest
export type DeleteMarkerVariables = MapMarkerVariables & MarkerIdentity
