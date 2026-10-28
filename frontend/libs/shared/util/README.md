# Shared utilities

Small utilities shared by both platforms. Tagged type:util, scope:shared and platform:shared; this internal library is not buildable.

Import toPromise from @game-atlas/shared/util at TanStack query/mutation transport boundaries. It resolves the first Observable emission and unsubscribes. An optional AbortSignal rejects with signal.reason and unsubscribes the source, cancelling an active HttpClient request. An already-aborted signal prevents subscription. Observable errors propagate unchanged.

getDirtyValues(fields) accepts a Signal Forms FieldTree and returns Partial<T> containing only dirty top-level fields. It preserves null, false, zero and empty strings; dirty object/array values are included whole. It imports Angular types only, with no runtime Angular dependency. Programmatic edits must call markAsDirty(); successful saves/discards reset the form baseline. Nested merge-patch serialization is not introduced by this helper.
