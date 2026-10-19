# GitHub publication

Publish the source using Git, so `.gitignore` is respected. Do not upload the project folder as a ZIP or drag all files into GitHub: that can include private files which Git excludes.

## First publication

From a terminal in the repository root:

```powershell
git init
git add .
git status --short
git diff --cached --stat
```

Review the staged list before committing. It must exclude `.demo-local`, `Uploads` documents, the legacy root SQL dump, database files/backups, local configuration, build output and restored packages. Keep third-party copyright and license notices.

```powershell
git commit -m "Add Africa University student election portal"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/YOUR-REPOSITORY.git
git push -u origin main
```

Create an empty repository first, and replace the remote URL above. If private files were committed previously, adding `.gitignore` does not remove them from Git history. Review and clean that history before making the repository public, and rotate exposed credentials.

## Recruiter setup

Follow [SETUP.md](SETUP.md) to restore dependencies, initialize an empty database and create your own local administrator. Then use [Demo/README.md](Demo/README.md) for the full isolated demonstration. No public administrator credentials are supplied. GitHub Pages cannot run this backend.

## Scope and rights

This is a portfolio/student election project. It needs further institutional eligibility integration and independent security review before a real university election. The normal application retains legacy ballot privacy limitations documented in [ARCHITECTURE.md](ARCHITECTURE.md).

Review [ASSETS.md](ASSETS.md): original university images are preserved under ignored `.private-assets/` and excluded from Git. Public SVG replacements have a scoped MIT licence. Do not upload private originals, old image previews or deployment folders. No blanket licence is applied to inherited application source.

