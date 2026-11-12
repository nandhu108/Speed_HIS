namespace Prixo.Domain.Enums;

/// <summary>Placeholder column per plan §11 ("virus-scan hook placeholder") — nothing sets this beyond NotScanned yet; see IVirusScanner.</summary>
public enum VirusScanStatus
{
    NotScanned = 1,
    Clean = 2,
    Infected = 3,
}
