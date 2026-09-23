import * as migration_20260922_192455 from './20260922_192455'

export const migrations = [
  {
    up: migration_20260922_192455.up,
    down: migration_20260922_192455.down,
    name: '20260922_192455',
  },
]
