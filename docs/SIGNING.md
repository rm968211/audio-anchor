# Code signing and the "unknown publisher" warning

Windows shows **"Publisher: Unknown"** in the installer's UAC prompt, and SmartScreen shows
"Windows protected your PC", because `SoundAnchor-<version>-win-x64-Setup.exe` carries no
Authenticode signature. Nothing in the app causes it; an unsigned binary always gets that warning.
Publishing checksums (`SHA256SUMS.txt`) proves integrity but does not remove the prompt — only a
certificate from a CA in the Windows Trusted Root program does.

## Options

| Option | Cost | Removes UAC "unknown publisher" | Removes SmartScreen prompt |
| --- | --- | --- | --- |
| Azure Trusted Signing | ~$10/month | Yes | Yes, quickly (Microsoft-operated CA) |
| SignPath Foundation (open source projects) | Free | Yes | After reputation builds |
| Certum / Sectigo / DigiCert OV certificate | ~$200-400/year, hardware token or HSM required | Yes | After reputation builds |
| EV certificate | ~$400+/year | Yes | Immediately |
| Self-signed certificate | Free | Only on machines that trust the certificate | No |
| Stay unsigned | Free | No (users choose **More info → Run anyway**) | No |

Since 2023 the CA/Browser Forum requires OV code-signing keys to live on a hardware token or HSM,
so a plain PFX file from a public CA is no longer available. Azure Trusted Signing (an
organisation or an individual with a verifiable identity) and SignPath Foundation (this repository
is public, which SignPath Foundation requires) are the cheapest routes for this project.

Reputation is per-certificate, not per-application: SmartScreen may still warn on the first
downloads of a newly issued OV certificate until enough installs accumulate.

## Enabling signing in this repository

Packaging signs `SoundAnchor.exe`, the setup and the uninstaller when it is given a signing command.
Nothing changes for unsigned builds.

```powershell
./scripts/Build-Packages.ps1 -InnoCompiler .tools/inno/ISCC.exe `
  -SignToolCommand 'signtool.exe sign /fd sha256 /tr http://timestamp.digicert.com /td sha256 /sha1 <thumbprint> $q$f$q'
```

The command uses Inno Setup's placeholders, because the same string signs the published executable
and is handed to Inno Setup for the setup and uninstaller: `$f` is the file being signed and `$q` is
a double quote. Write paths that contain spaces as `$qC:\Program Files\...$q`, and always wrap the
file as `$q$f$q`. Any signing tool works: `signtool.exe` with a token or HSM, `AzureSignTool`, or a
vendor CLI. Always include a timestamp (`/tr`) so signatures stay valid after the certificate
expires. Quote the whole `-SignToolCommand` value in single quotes so PowerShell does not expand
`$f` and `$q` itself.

In GitHub Actions, put the same command line in a repository secret named `SIGNTOOL_COMMAND`. The
**Build, test and package** job passes it through automatically, so merged PRs publish signed
releases; without the secret, builds stay unsigned. Any credential the command needs (for example
Azure Trusted Signing environment variables) must also be configured on the runner as secrets.
Never commit a certificate, token PIN, or signing credential to this repository.

## Verifying a signed build

```powershell
Get-AuthenticodeSignature .\artifacts\packages\SoundAnchor-<version>-win-x64-Setup.exe |
  Format-List Status, SignerCertificate, TimeStamperCertificate
```

`Status` must be `Valid`. Also check the UAC prompt on a clean machine: it must name the publisher
instead of "Unknown". Signing cannot be verified in CI without a certificate, so record the result
of this manual check in [HANDOFF.md](HANDOFF.md) when signing is first enabled.
