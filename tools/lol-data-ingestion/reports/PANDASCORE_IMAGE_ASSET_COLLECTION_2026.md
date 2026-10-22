# PandaScore Image Asset Collection 2026

Source: PandaScore.

Status: **PARTIAL**. URL indexing is not a completed image collection.

## Coverage

| Scope | Entities | URLs | Verified cached | Missing URL | Failed | Invalid | Rights review |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Catalog players | 9,915 | 5,598 | 5,598 | 4,317 | 0 | 0 | 0 |
| Regular team logos | 1,948 | 1,640 | 1,640 | 308 | 0 | 0 | 0 |
| Dark team logos | 1,948 | 174 | 174 | 1,774 | 0 | 0 | 0 |
| League logos | 136 | 118 | 118 | 18 | 0 | 0 | 0 |
| 2026 registered players | 2,331 | 1,126 | 1,126 | 1,205 | 0 | 0 | 0 |
| 2026 competition teams (regular logo) | 363 | 357 | 357 | 6 | 0 | 0 | 0 |

Coverage uses stable provider IDs. Competition teams include stage teams and match opponents.
Cached means the original URL, entity-specific relative path, SHA-256, size, format, MIME and decoded image were validated.
Null paths are preserved; dark-logo selection can use only the same team's regular logo.

## Associations

| Check | Count |
| --- | ---: |
| player_mappings | 9,915 |
| team_mappings | 1,948 |
| valid_player_image_references | 5,598 |
| valid_team_logo_references | 1,640 |
| roster_rows | 9,764 |
| valid_roster_entity_links | 9,764 |
| roster_links_with_both_images_cached | 6,444 |
| roster_rows_missing_player_url | 3,311 |
| roster_rows_missing_team_url | 35 |
| Broken reference: image_entities | 0 |
| Broken reference: registration_tournaments | 0 |
| Broken reference: registration_players | 0 |
| Broken reference: registration_teams | 0 |
| Broken reference: current_teams | 1 |
| Broken reference: competition_teams | 0 |
| overlapping_registration_pairs | 190 |
| overlapping_registration_players | 97 |
| unknown_period_review_pairs | 0 |
| historical_revision_reviews | 24 |
| player_index_disagreement | 0 |
| absent_image_reference_rows | 0 |

Player portraits resolve by player ID; tournament logos resolve by registration team ID.
The provider's current team and append-only team/registration observations remain separate evidence.
Overlapping tournament periods under different teams are review flags, not asserted errors or transfer dates.
Unknown dates are flagged separately. No match participation, active contract, academy or reserve status is inferred.
No normalization or REST collection was rerun.

## Download and integrity

| Metric (latest invocation) | Value |
| --- | ---: |
| mode | dry-run |
| started_at | 2026-10-14T08:55:04.175649+00:00 |
| duration_seconds | 106.312 |
| candidates | 7530 |
| planned | 0 |
| requested | 0 |
| http_requests | 0 |
| response_bytes | 0 |
| downloaded_bytes | 0 |
| cached | 0 |
| unchanged | 0 |
| skipped_known | 7530 |
| skipped_failed | 0 |
| failed | 0 |
| retries | 0 |
| interrupted | False |
| license_status | LICENSE_REVIEW_REQUIRED |
| Verified cache files (all runs) | 7,530 |
| Verified cache bytes (all runs) | 1,292,868,384 |
| URL assets still uncached | 0 |

Invocation metrics are not cumulative; data/assets/runs/*.json preserves each checkpointed run.
The database commits every completed attempt. A stopped run resumes with the same command; verified hits and
recorded failures do not consume --limit. --retry-failed explicitly revisits failures with bounded retries.
Corrupt cached files and changed source URLs are eligible for refetch. --refresh only rechecks valid hits.

## Rights decision (reviewed 2026-10-14, Asia/Saigon)

[PandaScore terms, articles 6.2–6.5](https://www.pandascore.co/terms-and-condition) tie study/analysis use to the subscription,
restrict raw-data transfers, require attribution even internally, and require use to stop after termination.
The reviewed public terms do not establish this account's permission to archive player photographs or third-party marks.
Repository reports describe Free-plan access. A user execution approval is not an account-specific image grant.
References with a recorded execution approval: 7,530. See licence notes in the ignored manifest/database.

- Internal research caching: execution may be authorized by a recorded user decision; external rights remain a separate question.
- Local visualization/prototyping: separately confirm permitted display and attribution.
- Public redistribution: not established; raw-data transfer restrictions apply.
- Commercial game distribution: not authorized by this work; requires separate applicable rights.

Required evidence: subscription/account terms or written provider/rightsholder confirmation covering bulk local caching
of portraits and regular/dark/league logos, internal users, retention, attribution and any display scope, followed by
a responsible approver's dated decision referencing that evidence. --license-approved records this decision; it does not create rights.
Download execution has proceeded under the recorded user instruction. No provider/rightsholder licence is inferred.

## CDN and validation

[Image optimization documentation](https://developers.pandascore.co/docs/image-optimization) describes original images and
800/200-pixel team variants on cdn.pandascore.co. Database URL observations:

- Hosts: `{"cdn-api.pandascore.co": 7530}`.
- Path extensions: `{".png": 7460, "no_extension": 70}`.

URL suffixes do not prove response MIME/format.
Verified cached formats: `{"png": 7528, "webp": 2}`.
The downloader accepts validated PNG, JPEG, GIF and WebP
and preserves received bytes/format. It constructs no thumbnail or normalized URL.
HTTPS, host/redirect checks, timeouts, 8 MiB / 40-megapixel caps, per-request pacing, bounded retries, image signatures, MIME checks,
bounded decoding, atomic writes and SHA-256 checks apply. SVG and HTML are rejected.
[Pillow validation documentation](https://pillow.readthedocs.io/en/stable/reference/Image.html) informs verify/load and pixel safeguards.

## Outputs and execution

All paths below are relative to tools/lol-data-ingestion/; provider-level exports and bytes are Git-ignored.

- data/staging.sqlite3: existing image_cache table, additive migration; normalized source tables untouched.
- data/assets/image_manifest.json: unified identity, provenance, use status, download status and verified file metadata.
- data/assets/mapping/player_images.json
- data/assets/mapping/team_images.json
- data/assets/mapping/roster_image_index_2026.json
- data/assets/mapping/association_reviews_2026.json
- data/assets/mapping/historical_registration_reviews.json
- data/assets/image_coverage.json
- data/assets/runs/*.json: invocation metrics and checkpoints.
- data/assets/players/, team-logos/, league-logos/: created only for approved successful downloads.
  Files are named after the entity: players/<nickname>.<ext>, team-logos/<team>.<ext> and <team>-dark.<ext>,
  league-logos/<league>.<ext>. Names shared by several entities of one kind carry __<provider id> on every
  member. Spaces become underscores; the provider ID stays in every manifest entry and mapping row.

```powershell
.\.venv\Scripts\python.exe cache_logos.py --dry-run --include-dark
.\.venv\Scripts\python.exe collection_report.py --image-assets
```

For an explicitly authorized run, pass the actual scoped decision and its evidence reference to --license-approved:

```powershell
.\.venv\Scripts\python.exe cache_logos.py --license-approved $approvalNote --pilot
.\.venv\Scripts\python.exe cache_logos.py --license-approved $approvalNote --include-dark --limit 250 --batch-size 50
```

Repeat the bounded full command to advance through pending assets. Inspect the pilot manifest, mappings and integrity first.
Rebuild with --dry-run after collection for whole-cache validation. Never copy provider assets into production content.
