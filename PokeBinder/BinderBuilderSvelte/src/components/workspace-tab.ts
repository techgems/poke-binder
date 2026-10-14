// Which panel the workspace is showing. The sidebar and the panel both steer it, so neither owns
// it.

/** The panels the workspace can show. Exactly one is ever selected. */
export type WorkspaceTab = 'add' | 'binder' | 'set' | 'settings'

/** The Card Search panel is where cards come from, so it is where the workspace starts. */
export const DEFAULT_WORKSPACE_TAB: WorkspaceTab = 'add'
