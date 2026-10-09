# full-r2 paw-only diagnostic

Status: `diagnostic_pending_full_visual_review_not_for_game`.

This package applies the previously reviewed interior paw Alpha repair to all 121 existing native frames (000-120). It keeps the original 960x960 source index and timing, does not delete or repeat frames, and does not include the r3 mane RGB candidate. The source RGB green detail, all Alpha outside the enclosed paw mask, visible support, and mane RGBA remain unchanged.

The independent verification passed 121/121 source, baseline, candidate, protection, mask, and timing checks. The measured aggregate has 14,166 paw Alpha pixels restored to 255 and 17,195 paw RGB pixels adjusted using bounded local source chroma estimates; enclosed low Alpha remaining in the paw ROI is 0. This is a file/protection result, not proof that the source material detail was recovered.

`animations/gray-full.webp` retains the native 41/42ms timing and 5,042ms total. `animations/gray-full.gif` contains the same 121 frames with 10ms cumulative timestamp quantization and 5,040ms total. Half and quarter speed variants are included and verified. `contact-sheets/` contains 20 native local before/after sheets; each image is below 1400px. The package is approximately 92.3MiB because it preserves 121 native PNGs, six requested animation previews, masks, and contacts.

Visual review of all native color/Alpha details, edge quality, motion playback, loop seam, engine sampling, and Unity import remain pending. The full-r2 output is not approved for game use. The old matte failure verdict remains active until independent visual review is complete.
