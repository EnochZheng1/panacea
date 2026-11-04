#!/bin/bash
# Capture the review suite: every screen of the real game (C# sim), plus motion clips with sound.
# Usage: tools/capture_suite.sh OUT_DIR [seed]
# Produces OUT_DIR/*.png (1280x800) and OUT_DIR/*.mp4, and OUT_DIR/INDEX.md describing each.
set -e
cd "$(dirname "$0")/.."
OUT=$(realpath -m "$1"); SEED=${2:-ward-7}
mkdir -p "$OUT"
export PATH=$HOME/.dotnet:$PATH DOTNET_ROOT=$HOME/.dotnet
dotnet build -nologo -v q | grep -E " error " && exit 1 || true
G() { xvfb-run -a -s "-screen 0 ${W:-1280}x${H:-800}x24" ~/tools/godot.sh --path . --rendering-driver vulkan --resolution ${W:-1280}x${H:-800} "$@" 2>&1 | grep -E "Exception" || true; }
shot() { local name=$1; shift; G -- --shot "$OUT/$name.png" --frames ${F:-14} --seed $SEED "$@"; echo "- $name.png: $DESC" >> "$OUT/INDEX.md"; }
clip() { local name=$1 frames=$2; shift 2; local d; d=$(mktemp -d); G --write-movie "$d/f.png" --fixed-fps 30 --quit-after $frames -- --seed $SEED "$@"; ffmpeg -loglevel error -y -framerate 30 -i "$d/f%08d.png" -i "$d/f.wav" -c:v libx264 -pix_fmt yuv420p -crf 23 -c:a aac -shortest "$OUT/$name.mp4"; rm -rf "$d"; echo "- $name.mp4: $DESC" >> "$OUT/INDEX.md"; }
echo "# Panacea review suite (seed $SEED)" > "$OUT/INDEX.md"
DESC="the first edition of the Gazette, January 1885, as a new player sees it" H=1500 shot 01_first_edition_full
DESC="the first edition at the real 1280x800 window (what fits on screen)" shot 02_first_edition_window
DESC="the proof screen at the start of the first month (counter, type case, letters, works, press)" shot 03_proof_month1 --screen proof
DESC="the formula bench drawer in month 1" F=24 shot 04_formula_bench_month1 --screen proof --drawer formula
DESC="the still drawer in month 1" shot 05_still_month1 --screen proof --drawer still
DESC="the supplies drawer in month 1" shot 06_supplies_month1 --screen proof --drawer supplies
DESC="an edition after 5 months of a heavy-laudanum scripted proprietor (deaths, inquests)" H=1600 shot 07_edition_month5_heavy --play 5 --dose 1.5
DESC="the proof after 5 heavy months (Death at a customer's shoulder)" shot 08_proof_month5_heavy --play 5 --dose 1.5 --screen proof
DESC="the busiest proof: many claims, several inquest letters and two bench warnings (layout worst case)" SEED=careful-3 shot 08b_proof_busiest --play 7 --dose 1.0 --screen proof
DESC="the letters drawer with an inquest letter" shot 09_letters_inquest --play 5 --dose 1.5 --screen proof --drawer letters
DESC="a death-notice cutting opened from the paper" F=20 shot 10_clip_death --play 5 --dose 1.5 --clip death
DESC="the month's books cutting opened from the paper" F=20 shot 11_clip_books --play 5 --dose 1.5 --clip books
DESC="an edition after 24 mild months with half a column of advertising" H=1600 shot 12_edition_month24_mild --play 24 --dose 0.75 --ads 600
DESC="the proof after 24 mild months with ad space bought" shot 13_proof_month24_ads --play 24 --dose 0.75 --ads 600 --screen proof
DESC="the final edition of a game shut down by the Board of Health" H=1600 shot 14_ending_shutdown --play 12 --dose 3.0
DESC="the press moment with sound: proof to presses running to the printed edition and the knell" clip 20_press_moment 420 --play 4 --dose 1.5 --screen proof --autopress 3.0
DESC="pouring at the formula bench with sound: laudanum in, water out, gauges moving" clip 21_formula_pour 300 --play 3 --dose 0.25 --screen proof --drawer formula --pour laudanum,laudanum,laudanum,laudanum,laudanum,laudanum,-water,-water,-water,-water,-water,-water
DESC="idle on the proof screen with sound: the counter breathing and Death's breath" clip 22_counter_idle 150 --play 5 --dose 1.5 --screen proof
echo "done: $OUT"
