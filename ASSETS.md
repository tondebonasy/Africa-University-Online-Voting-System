# Assets, permissions and notices

## Public image assets

The public project uses three original neutral SVG illustrations created for this portfolio. They contain no third-party photographs, university seals, personal images or external resources.

| File | Use | Licence |
| --- | --- | --- |
| `OnlineVotingSystem/Content/images/election-mark.svg` | Open-book and voting-check emblem; header and favicon | MIT |
| `OnlineVotingSystem/Content/images/campus-aerial.svg` | Fictional campus garden from above; landing background | MIT |
| `OnlineVotingSystem/Content/images/campus-garden.svg` | Fictional courtyard at dusk; account-page background | MIT |

The [image licence](OnlineVotingSystem/Content/images/LICENSE.txt) permits use, modification and redistribution with its notice. It applies only to these three SVG files. They are editable source artwork; no image downloads or API keys are required to run the application. These illustrations do not depict a real campus and the emblem is not an official Africa University logo. The university name identifies the project's intended context, not endorsement.

## Original images retained locally and excluded

Originals were moved outside the web project to ignored `.private-assets/originals/`, preserving their relative paths. `.private-assets/originals-manifest.json` records the ten original paths and SHA-256 checksums. Each moved file was verified against its original checksum. These files remain on the developer's computer and are not part of a clone.

| Original path | Review | Public status |
| --- | --- | --- |
| `OnlineVotingSystem/Content/images/Africa_University_Logo.jpg` | University logo; redistribution permission unconfirmed | Preserved privately; excluded |
| `OnlineVotingSystem/Content/images/Africa_University_Logo.png` | Converted university logo; conversion does not confer rights | Preserved privately; excluded |
| `OnlineVotingSystem/Content/images/africa_university_arial_view.jpg` | Aerial campus photograph; photographer/licence unconfirmed | Preserved privately; excluded |
| `OnlineVotingSystem/Content/images/Login.jpg` | Campus photograph with distant person; licence/consent unconfirmed | Preserved privately; excluded |
| `OnlineVotingSystem/Content/images/church2.jpg` | Church photograph; provenance/licence unconfirmed | Preserved privately; excluded |
| `OnlineVotingSystem/Content/images/logo.jpeg` | Apostolic Faith Mission of Zimbabwe logo; unrelated branding | Preserved privately; excluded |
| `OnlineVotingSystem/favicon.ico` and `OnlineVotingSystem/Content/favicon.ico` | Previous branding icons; provenance unconfirmed | Both preserved privately; excluded |
| `OnlineVotingSystem/Properties/favicon.ico` | Legacy project icon; provenance unconfirmed | Preserved privately; excluded |
| `OnlineVotingSystem/Uploads/Login.jpg` | Duplicate campus photograph | Preserved privately; excluded |

Old design previews, uploads and ignored demo deployment copies can also contain the original imagery. They remain private and must not be uploaded. The `.gitignore` also excludes originals if they are restored to their former paths. Use Git to publish; uploading a whole folder can bypass these exclusions. Do not reintroduce originals without documenting the rights holder, grant/source licence and required attribution.

## Existing notices preserved

- `OnlineVotingSystem/Properties/AssemblyInfo.cs` retains its existing 2022 copyright notice.
- [Manrope OFL](OnlineVotingSystem/Content/fonts/OFL.txt) is retained with the font files.
- Bootstrap copyright/licence headers remain in the bundled CSS/JavaScript, including Popper notices.
- jQuery, jQuery Validation, unobtrusive validation, Modernizr and Glyphicons embedded notices remain intact. The licensed Glyphicons SVG font is not an original university image and is retained.
- Restored NuGet dependencies carry their upstream licences and are not committed as binaries.

The image licence is not a blanket licence over inherited application source. Existing source ownership and third-party terms remain unchanged.
