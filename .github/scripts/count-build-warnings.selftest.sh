#!/usr/bin/env bash
# Self-test for count-build-warnings.sh: one each of CS/CA/IDE/RZ/ASP/BL + a duplicate → 6 (NU190x excluded).
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
script="$root/.github/scripts/count-build-warnings.sh"
fixture="$(mktemp)"
trap 'rm -f "$fixture"' EXIT

cat >"$fixture" <<'LOG'
/workspace/Jobsy.Core/Foo.cs(1,1): warning CS0219: unused
/workspace/Jobsy.Core/Foo.cs(1,1): warning CS0219: unused
/workspace/Jobsy.Infrastructure/Bar.cs(2,2): warning CA1822: member
/workspace/Jobsy.Web/Baz.cs(3,3): warning IDE0060: param
/workspace/Jobsy.Web/Components/Page.razor(4,4): warning RZ10012: unexpected
/workspace/Jobsy.Web/Program.cs(5,5): warning ASP0000: something
/workspace/Jobsy.Web/BlazorThing.cs(6,6): warning BL0007: parameter
/workspace/Jobsy.Api/Jobsy.Api.csproj : warning NU1903: Package 'X' 1.0.0 has a known high severity vulnerability
LOG

got="$("$script" "$fixture")"
if [[ "$got" != "6" ]]; then
  echo "expected 6 unique warnings, got $got" >&2
  exit 1
fi
echo "count-build-warnings.selftest: ok ($got)"
