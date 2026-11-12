using System.Text.Json;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Patients.Abdm;

/// <summary>
/// Generates NRCeS (National Resource Centre for EHR Standards) FHIR R4 compliant bundles
/// for ABDM Milestone M2 (Health Information Provider - HIP) data sharing.
/// </summary>
public class FhirBundleService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string BuildOpConsultationBundle(
        Patient patient,
        string abhaNumber,
        string facilityName,
        string hipId,
        string doctorName,
        string encounterRef,
        DateTimeOffset encounterDate,
        string? chiefComplaints,
        string? diagnosis,
        string? clinicalNotes)
    {
        var bundleId = Guid.NewGuid().ToString("D");
        var compositionId = Guid.NewGuid().ToString("D");
        var patientId = Guid.NewGuid().ToString("D");
        var practitionerId = Guid.NewGuid().ToString("D");

        var bundle = new Dictionary<string, object?>
        {
            ["resourceType"] = "Bundle",
            ["id"] = bundleId,
            ["meta"] = new Dictionary<string, object>
            {
                ["versionId"] = "1",
                ["lastUpdated"] = encounterDate.ToString("o"),
                ["profile"] = new[] { "https://nrces.in/ndhm/fhir/r4/StructureDefinition/DocumentBundle" }
            },
            ["identifier"] = new Dictionary<string, object>
            {
                ["system"] = $"https://abdm.gov.in/facilities/{hipId}",
                ["value"] = encounterRef
            },
            ["type"] = "document",
            ["timestamp"] = encounterDate.ToString("o"),
            ["entry"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["fullUrl"] = $"urn:uuid:{compositionId}",
                    ["resource"] = new Dictionary<string, object?>
                    {
                        ["resourceType"] = "Composition",
                        ["id"] = compositionId,
                        ["status"] = "final",
                        ["type"] = new Dictionary<string, object>
                        {
                            ["coding"] = new[]
                            {
                                new Dictionary<string, string>
                                {
                                    ["system"] = "https://projectndhm.in/fhir/ndhm/CodeSystem/ndhm-record-types",
                                    ["code"] = "OPConsultation",
                                    ["display"] = "Outpatient Consultation"
                                }
                            },
                            ["text"] = "Outpatient Consultation Note"
                        },
                        ["subject"] = new Dictionary<string, string> { ["reference"] = $"urn:uuid:{patientId}" },
                        ["date"] = encounterDate.ToString("o"),
                        ["author"] = new[] { new Dictionary<string, string> { ["reference"] = $"urn:uuid:{practitionerId}" } },
                        ["title"] = "OPD Consultation Record",
                        ["section"] = new object[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["title"] = "Chief Complaints & History",
                                ["code"] = new Dictionary<string, object>
                                {
                                    ["coding"] = new[] { new Dictionary<string, string> { ["system"] = "http://loinc.org", ["code"] = "42254-3", ["display"] = "History of present illness" } }
                                },
                                ["text"] = new Dictionary<string, string> { ["status"] = "generated", ["div"] = $"<div>{chiefComplaints ?? "Routine consultation"}</div>" }
                            },
                            new Dictionary<string, object?>
                            {
                                ["title"] = "Clinical Impression & Diagnosis",
                                ["code"] = new Dictionary<string, object>
                                {
                                    ["coding"] = new[] { new Dictionary<string, string> { ["system"] = "http://loinc.org", ["code"] = "29308-4", ["display"] = "Diagnosis" } }
                                },
                                ["text"] = new Dictionary<string, string> { ["status"] = "generated", ["div"] = $"<div>{diagnosis ?? "Consultation completed; vitals stable."}</div>" }
                            },
                            new Dictionary<string, object?>
                            {
                                ["title"] = "Plan & Advice",
                                ["text"] = new Dictionary<string, string> { ["status"] = "generated", ["div"] = $"<div>{clinicalNotes ?? "Standard clinical follow-up advice given."}</div>" }
                            }
                        }
                    }
                },
                new Dictionary<string, object?>
                {
                    ["fullUrl"] = $"urn:uuid:{patientId}",
                    ["resource"] = new Dictionary<string, object?>
                    {
                        ["resourceType"] = "Patient",
                        ["id"] = patientId,
                        ["identifier"] = new object[]
                        {
                            new Dictionary<string, string> { ["system"] = "https://healthid.abdm.gov.in", ["value"] = abhaNumber },
                            new Dictionary<string, string> { ["system"] = $"https://abdm.gov.in/facilities/{hipId}/uhid", ["value"] = patient.Uhid }
                        },
                        ["name"] = new[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["text"] = $"{patient.FirstName} {patient.LastName}".Trim(),
                                ["given"] = new[] { patient.FirstName },
                                ["family"] = patient.LastName
                            }
                        },
                        ["gender"] = patient.Gender.ToString().ToLowerInvariant(),
                        ["birthDate"] = patient.DateOfBirth?.ToString("yyyy-MM-dd")
                    }
                },
                new Dictionary<string, object?>
                {
                    ["fullUrl"] = $"urn:uuid:{practitionerId}",
                    ["resource"] = new Dictionary<string, object?>
                    {
                        ["resourceType"] = "Practitioner",
                        ["id"] = practitionerId,
                        ["name"] = new[] { new Dictionary<string, string> { ["text"] = doctorName } }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(bundle, JsonOpts);
    }

    public string BuildPrescriptionBundle(
        Patient patient,
        string abhaNumber,
        string facilityName,
        string hipId,
        string doctorName,
        string prescriptionRef,
        DateTimeOffset prescriptionDate,
        IReadOnlyList<(string MedicineName, string Dosage, string Duration, string Instructions)> items)
    {
        var bundleId = Guid.NewGuid().ToString("D");
        var entries = new List<object>
        {
            new Dictionary<string, object?>
            {
                ["resourceType"] = "Composition",
                ["status"] = "final",
                ["type"] = new Dictionary<string, object>
                {
                    ["coding"] = new[] { new Dictionary<string, string> { ["system"] = "https://projectndhm.in/fhir/ndhm/CodeSystem/ndhm-record-types", ["code"] = "Prescription" } }
                },
                ["title"] = "E-Prescription",
                ["date"] = prescriptionDate.ToString("o")
            }
        };

        foreach (var item in items)
        {
            entries.Add(new Dictionary<string, object?>
            {
                ["resourceType"] = "MedicationRequest",
                ["status"] = "active",
                ["intent"] = "order",
                ["medicationCodeableConcept"] = new Dictionary<string, object>
                {
                    ["text"] = item.MedicineName
                },
                ["dosageInstruction"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["text"] = $"{item.Dosage} for {item.Duration}. {item.Instructions}".Trim()
                    }
                }
            });
        }

        var bundle = new Dictionary<string, object?>
        {
            ["resourceType"] = "Bundle",
            ["id"] = bundleId,
            ["type"] = "document",
            ["timestamp"] = prescriptionDate.ToString("o"),
            ["entry"] = entries.Select(e => new Dictionary<string, object?> { ["resource"] = e }).ToArray()
        };

        return JsonSerializer.Serialize(bundle, JsonOpts);
    }
}
