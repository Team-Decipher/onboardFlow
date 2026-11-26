import OrganisationDetailsPage from "../../../organisations/[id]/page";

export default function WorkspaceClientOrganisationDetailsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  return <OrganisationDetailsPage params={params} />;
}
