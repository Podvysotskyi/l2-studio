import { readBody } from 'h3'
import { moveStorageEntry } from '../../../utils/storage'
import {
  requireResourceStorage,
  storageRequest
} from '../../../utils/storage-request'

export default defineEventHandler(async event => {
  const request = await storageRequest(event)
  requireResourceStorage(request.kind)
  const body = await readBody<{
    path?: string
    destination?: string
    overwrite?: boolean
  }>(event)
  await moveStorageEntry(
    request.root,
    body?.path,
    body?.destination,
    body?.overwrite === true
  )
  setResponseStatus(event, 204)
})
