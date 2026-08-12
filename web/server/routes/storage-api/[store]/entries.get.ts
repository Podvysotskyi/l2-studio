import { getQuery } from 'h3'
import { listStorage } from '../../../utils/storage'
import { storageRequest } from '../../../utils/storage-request'

export default defineEventHandler(async event => {
  const request = await storageRequest(event)
  return listStorage(request.root, getQuery(event).path, request.kind)
})
