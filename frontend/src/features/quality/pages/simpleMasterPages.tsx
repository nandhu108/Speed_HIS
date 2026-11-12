import { endpoints } from '../../../api/endpoints'
import { SimpleMasterPage } from '../../masters/pages/SimpleMasterPage'

const q = endpoints.quality

export const CommitteesPage = () => (
  <SimpleMasterPage
    title="Committees"
    subtitle="Quality/NABH committees — Quality Assurance, Infection Control, Medication Management…"
    noun="committee"
    queryKey="quality-committees"
    endpoint={q.committees}
    managePermission="QUALITY.COMMITTEE.MANAGE"
  />
)

export const NabhStandardsPage = () => (
  <SimpleMasterPage
    title="NABH Standards"
    subtitle='Chapter/standard reference codes for evidence and non-conformance findings — use "CHAPTER_STANDARD" as the code, e.g. AAC_1'
    noun="NABH standard"
    queryKey="quality-nabh-standards"
    endpoint={q.nabhStandards}
    managePermission="QUALITY.COMMITTEE.MANAGE"
  />
)
