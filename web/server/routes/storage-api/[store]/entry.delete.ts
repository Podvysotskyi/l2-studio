import { getQuery } from 'h3'
import { deleteStorageEntry } from '../../../utils/storage'
import {
  requireResourceStorage,
  storageRequest
} from '../../../utils/storage-request'

export default defineEventHandler(async event => {
  const request = await storageRequest(event)
  requireResourceStorage(request.kind)
  await deleteStorageEntry(request.root, getQuery(event).path)
  setResponseStatus(event, 204)
})
