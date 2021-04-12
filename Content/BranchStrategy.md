# Branch Strategy #

There are three branches, plus release branches. The more 'static' branches are:
- `master`
- `develop`
- `LibraryReferenes`

The `develop` branch is the default branch. When working on a new release, create a 
version specific branch off of `develop`. Once it is implemented and tested, merge 
it back to `develop`.

The `LibraryReferences` branch has replaced the nuget references of libraries used, with
project references. To get this branch to compile, you need the corresponding librariy
repositories available in parallel folders to the IdApp folder.

## Creating a release ##

1. Merge the specific commit in the `develop` branch that is the 'release-to-be' into
the `master` branch. **NOTE**: use `squash merge`, so each feature is neatly packed into one merge commit.
2. Now create a branch off of `master` with a version specific name, like `v2.19_Android__10_iOS9´ or similar.

After a few releases the branch tree will look something like:
- `master`
  - `v2.19_Android10_iOS9`
  - `v2.18_Android10_iOS9`
  - `v2.17_Android10_iOS9`
