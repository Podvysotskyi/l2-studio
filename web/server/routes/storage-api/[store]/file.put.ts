import { getQuery } from 'h3'
import { writeStorageFile } from '../../../utils/storage'
import {
  requireCanonicalResourceFile,
  requireResourceStorage,
  storageRequest
} from '../../../utils/storage-request'

export default defineEventHandler(async event => {
  const request = await storageRequest(event)
  requireResourceStorage(request.kind)
  const query = getQuery(event)
  requireCanonicalResourceFile(query.path)
  await writeStorageFile(
    request.root,
    query.path,
    event.node.req,
    query.overwrite === 'true'
  )
  setResponseStatus(event, 204)
})
