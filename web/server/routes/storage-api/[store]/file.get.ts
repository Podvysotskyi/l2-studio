import { getQuery, sendStream, setHeader } from 'h3'
import { readableStorageFile } from '../../../utils/storage'
import { storageRequest } from '../../../utils/storage-request'

export default defineEventHandler(async event => {
  const request = await storageRequest(event)
  const file = await readableStorageFile(request.root, getQuery(event).path)
  setHeader(event, 'content-type', 'application/octet-stream')
  setHeader(event, 'content-length', file.size)
  setHeader(
    event,
    'content-disposition',
    `attachment; filename*=UTF-8''${encodeURIComponent(file.name)}`
  )
  return sendStream(event, file.stream)
})
