import { PageHeader } from '../../../components/ui/PageHeader'
import { SimpleMasterSection, type SimpleMasterSectionProps } from '../components/SimpleMasterSection'

export interface SimpleMasterPageProps extends SimpleMasterSectionProps {
  title: string
  subtitle: string
}

/** Generic standalone page for code/name/description lookup masters. */
export function SimpleMasterPage({ title, subtitle, ...section }: SimpleMasterPageProps) {
  return (
    <>
      <PageHeader title={title} subtitle={subtitle} />
      <SimpleMasterSection {...section} />
    </>
  )
}
