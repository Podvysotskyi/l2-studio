import { readBody } from 'h3'
import { createStorageDirectory } from '../../../utils/storage'
import {
  requireResourceStorage,
  storageRequest
} from '../../../utils/storage-request'

export default defineEventHandler(async event => {
  const request = await storageRequest(event)
  requireResourceStorage(request.kind)
  const body = await readBody<{ path?: string }>(event)
  await createStorageDirectory(request.root, body?.path)
  setResponseStatus(event, 204)
})
