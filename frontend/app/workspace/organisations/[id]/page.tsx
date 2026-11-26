import OrganisationDetailsPage from "../../../organisations/[id]/page";

export default function WorkspaceOrganisationDetailsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  return <OrganisationDetailsPage params={params} />;
}
