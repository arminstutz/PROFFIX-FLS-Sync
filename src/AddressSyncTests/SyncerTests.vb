Imports FlsGliderSync.ClubDomainService
Imports FlsGliderSync.AuthenticationDomainService
Imports FlsGliderSync
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.ServiceModel
Imports System.Threading
Imports FlsGliderSync.PersonDomainService

<TestClass()> Public Class SyncerTests
    Private Property AuthenticationDomainClient As FLSAuthenticationDomainServicesoapClient
    Private Property Syncer As Syncer

    Public Sub New()
        Dim binding As New BasicHttpBinding(),
            endpointBehavior As New SyncerEndpointBehavior()

        binding.MaxReceivedMessageSize = 999999

        AuthenticationDomainClient = New FLSAuthenticationDomainServicesoapClient(binding,
                                                                                   New EndpointAddress(
                                                                                       "http://test.glider-fls.ch/Services/FLS-Server-Service-FLSAuthenticationDomainService.svc/soap"))
        AuthenticationDomainClient.Endpoint.Behaviors.Add(endpointBehavior)
        AuthenticationDomainClient.Open()
        Assert.IsTrue(AuthenticationDomainClient.Login("fgzo",
                                                            "fgzo", True,
                                                            String.Empty).RootResults.Count > 0)

        Dim _
            personDomainService As _
                New PersonDomainServicesoapClient(binding, New EndpointAddress("http://test.glider-fls.ch/Services/FLS-Server-Service-PersonDomainService.svc/soap"))
        personDomainService.Endpoint.Behaviors.Add(endpointBehavior)

        Dim _
            clubDomainService As _
                New ClubDomainServicesoapClient(binding, New EndpointAddress("http://test.glider-fls.ch/Services/FLS-Server-Service-ClubDomainService.svc/soap"))
        clubDomainService.Endpoint.Behaviors.Add(endpointBehavior)

        Syncer = New Syncer(DateTime.MinValue, personDomainService, clubDomainService, "C:\\temp\\backUp.sbs")
        Syncer.Open()

        Thread.Sleep(2000)
    End Sub

    <TestMethod()> Public Sub TestSubmitPersonChanges()
        Dim person As PersonDetails = GetPersonByMemberKey("1595")(0),
            adressLine As String = person.AddressLine1(person.AddressLine1.Length - 1) + person.AddressLine1.Substring(0, person.AddressLine1.Length - 1)
        person.AddressLine1 = adressLine
        Dim changeset = Syncer.PersonDomainClient.SubmitChanges({New PersonDomainService.ChangeSetEntry() With {
            .Operation = PersonDomainService.DomainOperation.Update,
            .Entity = person
        }})
        Assert.IsTrue(GetPersonByMemberKey("1595")(0).AddressLine1 = adressLine)
    End Sub

    <TestMethod()> Public Sub TestSubmitClubChanges()
        Dim person As PersonDetails = GetPersonByMemberKey("1595")(0),
            isGliderInstructor = Not person.OwnClubRelatedPersonDetails.IsGliderInstructor
        person.OwnClubRelatedPersonDetails.IsGliderInstructor = isGliderInstructor

        Dim changeSet2 = Syncer.PersonDomainClient.SubmitChanges({New PersonDomainService.ChangeSetEntry() With {
                                                               .Operation = PersonDomainService.DomainOperation.Update,
                                                               .Entity = person}})
        Assert.IsTrue(GetPersonByMemberKey("1595")(0).OwnClubRelatedPersonDetails.IsGliderInstructor = isGliderInstructor)
    End Sub

    <TestMethod()> Public Sub TestGetPersonByMemberKey()
        Assert.IsTrue(GetPersonByMemberKey("1595")(0).OwnClubRelatedPersonDetails.MemberKey = "1595")
    End Sub


    Public Function GetPersonByMemberKey(id As String) As PersonDetails()
        Dim persons As PersonDetails() = Syncer.PersonDomainClient.GetPersonDetailsModifiedSince(DateTime.MinValue).RootResults.ToArray(),
            personCollection As New List(Of PersonDetails)
        For Each p As PersonDetails In persons
            Dim list As New List(Of ClubRelatedPersonDetails)(p.OtherClubRelatedPersonDetails)
            list.Add(p.OwnClubRelatedPersonDetails)
            For Each c As ClubRelatedPersonDetails In list
                If c.MemberKey = id Then
                    personCollection.Add(p)
                End If
            Next
        Next
        Return personCollection.ToArray()
    End Function

End Class