import { endpoints } from '../../../api/endpoints'
import { SimpleMasterPage } from './SimpleMasterPage'

const m = endpoints.masters

/** Route-level instances of the generic lookup page, one per simple master. */

export const SpecialtiesPage = () => (
  <SimpleMasterPage
    title="Specialties"
    subtitle="Medical specialties assigned to doctors"
    noun="specialty"
    queryKey="specialties"
    endpoint={m.specialties}
    managePermission="MASTER.SPECIALTY.MANAGE"
  />
)


export const UomsPage = () => (
  <SimpleMasterPage
    title="Units of Measure"
    subtitle="Units used by inventory items and drugs"
    noun="unit"
    queryKey="uoms"
    endpoint={m.uoms}
    managePermission="MASTER.UOM.MANAGE"
  />
)

export const SampleTypesPage = () => (
  <SimpleMasterPage
    title="Sample Types"
    subtitle="Specimen types collected for laboratory tests"
    noun="sample type"
    queryKey="sample-types"
    endpoint={m.sampleTypes}
    managePermission="MASTER.SAMPLETYPE.MANAGE"
  />
)

export const ModalitiesPage = () => (
  <SimpleMasterPage
    title="Modalities"
    subtitle="Radiology equipment modalities such as X-Ray, CT, MRI"
    noun="modality"
    queryKey="modalities"
    endpoint={m.modalities}
    managePermission="MASTER.MODALITY.MANAGE"
  />
)

export const DocumentTypesPage = () => (
  <SimpleMasterPage
    title="Document Types"
    subtitle="Categories for MRD scanned documents, such as Discharge Summary or Consent Form"
    noun="document type"
    queryKey="document-types"
    endpoint={m.documentTypes}
    managePermission="MASTER.RECORDDOCUMENTTYPE.MANAGE"
  />
)

export const CampsPage = () => (
  <SimpleMasterPage
    title="Camps"
    subtitle="Health camps patients can be registered under"
    noun="camp"
    queryKey="camps"
    endpoint={m.camps}
    managePermission="MASTER.CAMP.MANAGE"
  />
)

export const StatesPage = () => (
  <SimpleMasterPage
    title="States"
    subtitle="States and union territories for patient and branch addresses"
    noun="state"
    queryKey="states"
    endpoint={m.states}
    managePermission="MASTER.STATE.MANAGE"
  />
)

export const CitiesPage = () => (
  <SimpleMasterPage
    title="Cities"
    subtitle="Cities for patient addresses — Areas map to a City"
    noun="city"
    queryKey="cities"
    endpoint={m.cities}
    managePermission="MASTER.CITY.MANAGE"
  />
)

export const NationalitiesPage = () => (
  <SimpleMasterPage
    title="Nationalities"
    subtitle="Nationality options for patient registration"
    noun="nationality"
    queryKey="nationalities"
    endpoint={m.nationalities}
    managePermission="MASTER.NATIONALITY.MANAGE"
  />
)

export const ReligionsPage = () => (
  <SimpleMasterPage
    title="Religions"
    subtitle="Religion options for patient registration"
    noun="religion"
    queryKey="religions"
    endpoint={m.religions}
    managePermission="MASTER.RELIGION.MANAGE"
  />
)

export const LanguagesPage = () => (
  <SimpleMasterPage
    title="Languages"
    subtitle="Preferred-language options for patient registration"
    noun="language"
    queryKey="languages"
    endpoint={m.languages}
    managePermission="MASTER.LANGUAGE.MANAGE"
  />
)

export const SpeciallyAbledCategoriesPage = () => (
  <SimpleMasterPage
    title="Specially Abled Categories"
    subtitle="Disability categories for patient tagging"
    noun="specially abled category"
    queryKey="specially-abled-categories"
    endpoint={m.speciallyAbledCategories}
    managePermission="MASTER.SPECIALLYABLEDCATEGORY.MANAGE"
  />
)

export const PatientHcTagsPage = () => (
  <SimpleMasterPage
    title="Patient HC Tagging"
    subtitle="Health-camp tags that can be attached to a patient"
    noun="HC tag"
    queryKey="patient-hc-tags"
    endpoint={m.patientHcTags}
    managePermission="MASTER.PATIENTHCTAG.MANAGE"
  />
)

export const PatientCaseTypesPage = () => (
  <SimpleMasterPage
    title="Patient Case Types"
    subtitle="Case-type classification for patient registration"
    noun="case type"
    queryKey="patient-case-types"
    endpoint={m.patientCaseTypes}
    managePermission="MASTER.PATIENTCASETYPE.MANAGE"
  />
)

export const MlcTypesPage = () => (
  <SimpleMasterPage
    title="MLC Types"
    subtitle="Medico-legal case types, such as RTA, Assault, or Poisoning"
    noun="MLC type"
    queryKey="mlc-types"
    endpoint={m.mlcTypes}
    managePermission="MASTER.MLCTYPE.MANAGE"
  />
)

export const MhcDoctorsPage = () => (
  <SimpleMasterPage
    title="MHC Doctors"
    subtitle="Doctor tagging list for Master Health Checkup / camp panels"
    noun="MHC doctor"
    queryKey="mhc-doctors"
    endpoint={m.mhcDoctors}
    managePermission="MASTER.MHCDOCTOR.MANAGE"
  />
)

export const LoyaltyProgramsPage = () => (
  <SimpleMasterPage
    title="Loyalty Programs"
    subtitle="Loyalty program / tier names"
    noun="loyalty program"
    queryKey="loyalty-programs"
    endpoint={m.loyaltyPrograms}
    managePermission="MASTER.LOYALTYPROGRAM.MANAGE"
  />
)

export const UnitsPage = () => (
  <SimpleMasterPage
    title="Units"
    subtitle="Hospital sub-units / wings, distinct from Department"
    noun="unit"
    queryKey="units"
    endpoint={m.units}
    managePermission="MASTER.UNIT.MANAGE"
  />
)

export const LoyaltyCardCategoriesPage = () => (
  <SimpleMasterPage
    title="Loyalty Card Categories"
    subtitle="Loyalty card category tags, e.g. Individual / Family / Corporate"
    noun="loyalty card category"
    queryKey="loyalty-card-categories"
    endpoint={m.loyaltyCardCategories}
    managePermission="MASTER.LOYALTYCARDCATEGORY.MANAGE"
  />
)

export const MemberRelationTagsPage = () => (
  <SimpleMasterPage
    title="Member Relation Tags"
    subtitle="Relation tags for loyalty card dependents, e.g. Spouse / Child / Parent"
    noun="member relation tag"
    queryKey="member-relation-tags"
    endpoint={m.memberRelationTags}
    managePermission="MASTER.MEMBERRELATIONTAG.MANAGE"
  />
)

export const SalutationsPage = () => (
  <SimpleMasterPage
    title="Salutations"
    subtitle="Name prefixes for patient registration, e.g. Mr / Mrs / Dr"
    noun="salutation"
    queryKey="salutations"
    endpoint={m.salutations}
    managePermission="MASTER.SALUTATION.MANAGE"
  />
)

export const MaritalStatusesPage = () => (
  <SimpleMasterPage
    title="Marital Statuses"
    subtitle="Marital status options for patient registration"
    noun="marital status"
    queryKey="marital-statuses"
    endpoint={m.maritalStatuses}
    managePermission="MASTER.MARITALSTATUS.MANAGE"
  />
)

export const IdentityDocumentTypesPage = () => (
  <SimpleMasterPage
    title="Identity Document Types"
    subtitle="Generalized ID document types, e.g. PAN / Passport / Voter ID / Driving License"
    noun="identity document type"
    queryKey="identity-document-types"
    endpoint={m.identityDocumentTypes}
    managePermission="MASTER.IDENTITYDOCUMENTTYPE.MANAGE"
  />
)

export const VisitClassesPage = () => (
  <SimpleMasterPage
    title="Visit Classes"
    subtitle="OP registration class tags, e.g. Regular / Express / Premium"
    noun="visit class"
    queryKey="visit-classes"
    endpoint={m.visitClasses}
    managePermission="MASTER.VISITCLASS.MANAGE"
  />
)

export const VisitSessionsPage = () => (
  <SimpleMasterPage
    title="Visit Sessions"
    subtitle="OP registration session tags, e.g. Morning / Afternoon / Evening"
    noun="visit session"
    queryKey="visit-sessions"
    endpoint={m.visitSessions}
    managePermission="MASTER.VISITSESSION.MANAGE"
  />
)

export const ClaimDocumentTypesPage = () => (
  <SimpleMasterPage
    title="Claim Document Types"
    subtitle="Supporting-document types for insurance claims, e.g. Pre-Auth Form / Bill Copy"
    noun="claim document type"
    queryKey="claim-document-types"
    endpoint={m.claimDocumentTypes}
    managePermission="MASTER.CLAIMDOCUMENTTYPE.MANAGE"
  />
)

export const ClaimDenialReasonsPage = () => (
  <SimpleMasterPage
    title="Claim Denial Reasons"
    subtitle="Standard reason codes for a denied insurance claim"
    noun="claim denial reason"
    queryKey="claim-denial-reasons"
    endpoint={m.claimDenialReasons}
    managePermission="MASTER.CLAIMDENIALREASON.MANAGE"
  />
)
