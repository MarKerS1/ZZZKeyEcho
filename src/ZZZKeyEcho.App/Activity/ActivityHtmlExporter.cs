using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZZZKeyEcho.App.Rendering;
using ZZZKeyEcho.Core.Activity;
using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.App.Activity;

public static class ActivityHtmlExporter
{
    public static void Export(string csvPath, string htmlPath, IconProvider icons)
    {
        var rows = ActivityCsv.Read(csvPath);
        var title = Path.GetFileNameWithoutExtension(csvPath);

        var names = ActionCatalog.All.ToDictionary(
            action => action.Id,
            action => action.DisplayName,
            StringComparer.Ordinal);

        names["assist"] = "支援技";

        var payload = JsonSerializer.Serialize(new
        {
            name = title,
            rows = rows.Select(row => new
            {
                p = row.PressedAt,
                a = row.ActionId,
                r = row.ReleasedAt,
            }),
        });

        var html = Template
            .Replace("__TITLE__", WebUtility.HtmlEncode(title))
            .Replace("__DATA__", payload)
            .Replace("__NAMES__", JsonSerializer.Serialize(names))
            .Replace("__ICONS__", JsonSerializer.Serialize(BuildIcons(rows, icons)));

        File.WriteAllText(htmlPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static Dictionary<string, string> BuildIcons(IReadOnlyList<ActivityRow> rows, IconProvider icons)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var actionId in rows.Select(row => row.ActionId).Distinct(StringComparer.Ordinal))
        {
            var source = icons.GetActionIcon(actionId);

            if (source is null && TryDirection(actionId, out var direction))
            {
                source = icons.GetDirectionIcon(direction);
            }

            if (source is BitmapSource bitmap)
            {
                result[actionId] = ToDataUri(bitmap);
            }
        }

        foreach (var directionId in new[] { "dirUp", "dirDown", "dirLeft", "dirRight", "dirUpLeft", "dirUpRight", "dirDownLeft", "dirDownRight", "dirRotate" })
        {
            if (result.ContainsKey(directionId) || !TryDirection(directionId, out var direction))
            {
                continue;
            }

            if (icons.GetDirectionIcon(direction) is BitmapSource image)
            {
                result[directionId] = ToDataUri(image);
            }
        }

        return result;
    }

    private static string ToDataUri(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
    }

    private static bool TryDirection(string actionId, out Direction direction)
    {
        switch (actionId)
        {
            case "dirUp": direction = Direction.Up; return true;
            case "dirDown": direction = Direction.Down; return true;
            case "dirLeft": direction = Direction.Left; return true;
            case "dirRight": direction = Direction.Right; return true;
            case "dirUpLeft": direction = Direction.UpLeft; return true;
            case "dirUpRight": direction = Direction.UpRight; return true;
            case "dirDownLeft": direction = Direction.DownLeft; return true;
            case "dirDownRight": direction = Direction.DownRight; return true;
            case "dirRotate": direction = Direction.Rotate; return true;
            default: direction = default; return false;
        }
    }

    private const string Template = """
<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>__TITLE__ · 操作记录</title>
<style>
:root{
  --bg:#08090A; --panel:#101316; --panel2:#0C0F11; --line:#ffffff14; --lineSoft:#ffffff09; --lineStrong:#ffffff1c;
  --fg:#F2F2ED; --muted:#8A9195; --accent:#F2FF3C; --accent-ink:#0A0C0D; --danger:#FF3B30; --chipBg:#ffffff07; --chipFg:#C9D0D2; --iconShadow:drop-shadow(0 1px 2px #000a); --panelA:#121619; --panelB:#0D1012; --thBg:#0F1315; --hover:#ffffff06; --activeBg:rgba(242,255,60,.13); --scan:rgba(255,255,255,.018); --glow1:#1C2427; --glow2:#1A1E14;
}
body[data-theme="light"]{
  --bg:#EFEDE4; --panel:#FFFFFF; --panel2:#F5F3EB; --line:#00000022; --lineSoft:#00000010; --lineStrong:#00000026;
  --fg:#15181A; --muted:#666E72; --accent:#C9D400; --accent-ink:#0B0C0A; --danger:#E5342B;
  --chipBg:#00000008; --chipFg:#3A4043; --iconShadow:none; --panelA:#FFFFFF; --panelB:#F7F5EC; --thBg:#FBFAF4;
  --hover:#00000006; --activeBg:rgba(201,212,0,.28); --scan:rgba(0,0,0,.028); --glow1:#E9E4D1; --glow2:#F3EFD9;
}
.btn.ghost{background:transparent;color:var(--fg);border:1px solid var(--line);clip-path:none;font-weight:700}
.btn.ghost:hover{border-color:var(--accent);color:var(--accent);filter:none}
*{box-sizing:border-box}
html,body{height:100%}
body{
  margin:0; padding:26px clamp(16px,3vw,44px) 40px; color:var(--fg);
  font:14px/1.55 "Bahnschrift","Segoe UI","Microsoft YaHei",sans-serif;
  background:
    repeating-linear-gradient(135deg, var(--scan) 0 2px, transparent 2px 11px),
    radial-gradient(1100px 520px at 12% -10%, var(--glow1) 0%, transparent 60%),
    radial-gradient(900px 500px at 100% 0%, var(--glow2) 0%, transparent 55%),
    var(--bg);
}
.topbar{display:flex;align-items:flex-end;justify-content:space-between;gap:20px;margin-bottom:18px;flex-wrap:wrap}
.brand{display:flex;align-items:center;gap:14px}
.brand .slash{width:10px;height:54px;background:var(--accent);clip-path:polygon(45% 0,100% 0,55% 100%,0 100%);box-shadow:0 0 24px -4px var(--accent)}
.kicker{font-size:10px;letter-spacing:.34em;color:var(--accent);font-weight:700;margin-bottom:2px}
h1{margin:0;font-size:clamp(20px,2.2vw,30px);font-weight:800;letter-spacing:.02em;font-style:italic}
.meta{display:flex;gap:8px;flex-wrap:wrap}
.topRight{display:flex;align-items:center;gap:12px;flex-wrap:wrap;justify-content:flex-end}
.chip{border:1px solid var(--line);background:var(--chipBg);padding:4px 10px;font-size:12px;color:var(--chipFg);font-variant-numeric:tabular-nums}
.panel{
  background:linear-gradient(180deg,var(--panelA) 0%,var(--panelB) 100%);
  border:1px solid var(--line); border-left:3px solid var(--accent);
  padding:16px 18px 18px; margin-bottom:18px;
  box-shadow:0 18px 40px #000a;
  clip-path:polygon(0 0,100% 0,100% calc(100% - 14px),calc(100% - 14px) 100%,0 100%);
}
.panelHead{display:flex;align-items:center;justify-content:space-between;gap:14px;flex-wrap:wrap;margin-bottom:10px}
.panelTitle{font-size:13px;font-weight:800;letter-spacing:.22em;text-transform:uppercase;color:#E9EDE6}
.panelTitle::before{content:"//";color:var(--accent);margin-right:8px;letter-spacing:0}
.controls{display:flex;align-items:center;gap:12px;flex-wrap:wrap}
.btn{
  background:var(--accent);color:var(--accent-ink);border:0;padding:7px 18px;font-weight:800;font-size:12.5px;
  letter-spacing:.08em;cursor:pointer;clip-path:polygon(9px 0,100% 0,calc(100% - 9px) 100%,0 100%);
  transition:filter .15s, transform .05s;
}
.btn:hover{filter:brightness(1.08)}
.btn:active{transform:translateY(1px)}
.btn.isPlaying{background:#FF6B4A;color:#140B07}
.zoomLabel{font-size:11px;color:var(--muted);letter-spacing:.12em}
input[type=range]{width:150px;accent-color:var(--accent);cursor:pointer}
.timeReadout{font-variant-numeric:tabular-nums;font-size:12px;color:var(--accent);min-width:74px;text-align:right}
.laneToggles{display:flex;gap:12px;flex-wrap:wrap;margin:2px 0 10px;padding:8px 10px;background:var(--chipBg);border:1px solid var(--line)}
.laneToggles label{display:flex;align-items:center;gap:6px;font-size:12px;color:var(--muted);cursor:pointer;user-select:none}
.laneToggles input{accent-color:var(--accent);cursor:pointer}
.laneToggles .dot{width:8px;height:8px;border-radius:1px;flex:none}
.grip{color:var(--accent);font-size:13px;line-height:1;margin-right:6px;letter-spacing:-1px}
.laneLabel{cursor:grab}
.laneLabel.dragging{opacity:.35}
.laneLabel.dropTarget{outline:1px dashed var(--accent);outline-offset:-2px}.tlBody{display:flex;border-top:1px solid var(--line);margin-top:4px}
.tlLabels{width:136px;flex:none;border-right:1px solid var(--line)}
.labelSpacer{height:26px;border-bottom:1px solid var(--line)}
.laneLabel{display:flex;align-items:center;gap:8px;padding:0 6px;font-size:12px;color:#A9B1B4;border-bottom:1px solid var(--lineSoft)}
.laneMark{width:8px;height:8px;border-radius:1px;flex:none;box-shadow:0 0 8px -1px currentColor}
.tlViewport{position:relative;flex:1;overflow:hidden;cursor:grab;touch-action:none;user-select:none}
.tlViewport.dragging{cursor:grabbing}
.tlInner{position:absolute;left:0;top:0;will-change:transform}
.ruler{position:absolute;left:0;top:0;height:26px;border-bottom:1px solid var(--line);width:100%}
.tick{position:absolute;top:0;height:26px;border-left:1px solid var(--lineStrong);padding-left:5px;font-size:10px;color:var(--muted);line-height:26px;white-space:nowrap;font-variant-numeric:tabular-nums}
.lane{position:absolute;left:0;right:0;border-bottom:1px solid var(--lineSoft)}
.bar{position:absolute;height:24px;top:10px;--c:#8A9195;opacity:.96}
.bar .strip{position:absolute;left:8px;right:8px;top:7px;height:10px;background:var(--c);border-radius:2px;z-index:1;border:1px solid #00000066}
.bar .cap{position:absolute;width:20px;height:20px;top:2px;object-fit:contain;filter:var(--iconShadow)}
.bar .cap.l{left:0;z-index:3}
.bar .cap.r{right:0;z-index:2}
.tap{position:absolute;top:10px;width:20px;height:24px;display:flex;align-items:center;justify-content:center;z-index:4}
.tap img{width:20px;height:20px;object-fit:contain}
.cluster{position:absolute;height:24px;display:flex;align-items:center;gap:2px;z-index:4}
.cluster img{width:20px;height:20px;object-fit:contain}
.cluster .badge{font-size:10px;font-weight:800;line-height:14px;padding:0 3px;border-radius:2px;background:var(--c);color:#0A0C0D}
.hoverLine{position:absolute;top:0;bottom:0;width:1px;background:var(--accent);box-shadow:0 0 8px var(--accent);opacity:0;pointer-events:none;z-index:6;transition:opacity .08s}
.tlInner.focusing .bar,.tlInner.focusing .tap,.tlInner.focusing .cluster{opacity:.18}
.tlInner.focusing .focus{opacity:1 !important}
.tlInner.soloing .bar,.tlInner.soloing .tap,.tlInner.soloing .cluster{opacity:.12}
.tlInner.soloing .solo{opacity:1 !important}.playhead{position:absolute;top:0;bottom:0;left:50%;width:2px;background:var(--danger);box-shadow:0 0 14px var(--danger);transform:translateX(-1px);pointer-events:none;z-index:5}
.hint{margin-top:10px;font-size:11px;color:var(--muted);letter-spacing:.06em}
.rowCount{font-size:11.5px;color:var(--muted);font-variant-numeric:tabular-nums}
.tableFilters.collapsed{display:none}
.filterBarRight{display:flex;align-items:center;gap:10px;margin-left:auto}
.miniBtn.isActive{background:var(--accent);color:var(--accent-ink);border-color:var(--accent)}.tableFilters{display:flex;gap:16px;flex-wrap:wrap;align-items:flex-start;margin:0 0 10px;padding:8px 10px;background:var(--chipBg);border:1px solid var(--line)}
.filterGroup{display:flex;flex-direction:column;gap:5px}
.filterGroup > label{display:flex;align-items:center;gap:6px;font-size:11px;letter-spacing:.08em;color:var(--fg);font-weight:700;cursor:pointer;user-select:none}
.filterItems{display:flex;flex-wrap:wrap;gap:4px 10px}
.filterItems label{display:flex;align-items:center;gap:4px;font-size:11.5px;color:var(--muted);cursor:pointer;user-select:none}
.filterItems img{width:16px;height:16px;object-fit:contain}
.tableFilters input{accent-color:var(--accent);cursor:pointer}
.filterActions{display:flex;gap:6px;margin-left:auto;align-items:flex-start}
.miniBtn{background:transparent;border:1px solid var(--line);color:var(--muted);font-size:11px;padding:3px 8px;cursor:pointer;font-family:inherit}
.miniBtn:hover{border-color:var(--accent);color:var(--fg)}.viewTabs{display:flex;gap:6px;margin:-4px 0 16px}
.viewTab{background:transparent;border:1px solid var(--line);color:var(--muted);padding:7px 18px;font-size:12px;font-weight:700;letter-spacing:.1em;cursor:pointer;font-family:inherit;clip-path:polygon(8px 0,100% 0,calc(100% - 8px) 100%,0 100%)}
.viewTab.active{background:var(--accent);color:var(--accent-ink);border-color:var(--accent)}
.hidden{display:none}
.tableWrapLarge{max-height:min(78vh,900px)}.tableWrap{max-height:min(64vh,720px);overflow:auto;border-top:1px solid var(--line)}
table{width:100%;border-collapse:collapse}
th,td{padding:9px 12px;text-align:left;border-bottom:1px solid var(--lineSoft);font-size:13px}
th{
  position:sticky;top:0;z-index:2;background:var(--thBg);color:var(--muted);
  font-size:10.5px;letter-spacing:.16em;text-transform:uppercase;font-weight:700;
  border-bottom:2px solid var(--accent);cursor:pointer;white-space:nowrap;user-select:none
}
th:hover{color:var(--fg)}
td.num{font-variant-numeric:tabular-nums;color:var(--chipFg);white-space:nowrap}
.cellAction{display:flex;align-items:center;gap:9px;white-space:nowrap}
.cellAction img{width:22px;height:22px;object-fit:contain}
tbody tr{cursor:pointer}
tbody tr:hover td{background:var(--hover)}
tbody tr.active td{background:var(--activeBg);box-shadow:inset 3px 0 0 var(--accent)}
tbody tr.active td.cellAction{color:var(--fg);font-weight:700}
.empty{color:var(--muted);text-align:center;padding:28px}
::-webkit-scrollbar{height:10px;width:10px}
::-webkit-scrollbar-thumb{background:var(--lineStrong)}
::-webkit-scrollbar-track{background:var(--lineSoft)}
</style>
</head>
<body>

<header class="topbar">
  <div class="brand">
    <span class="slash"></span>
    <div>
      <div class="kicker">ACTIVITY LOG</div>
      <h1>__TITLE__</h1>
    </div>
  </div>
  <div class="topRight">
    <button class="btn ghost" id="themeBtn">亮色</button>
    <div class="meta" id="meta"></div>
  </div>
</header>

<nav class="viewTabs">
  <button class="viewTab active" data-view="overview">时间轴</button>
  <button class="viewTab" data-view="table">表格</button>
</nav>

<div id="viewOverview">

<section class="panel">
  <div class="panelHead">
    <div class="panelTitle">时间轴</div>
    <div class="controls">
      <button class="btn ghost" id="replayBtn">重新播放</button>
      <button class="btn" id="playBtn">▶ 播放</button>
      <span class="zoomLabel">速度</span>
      <input id="speed" type="range" min="0.1" max="3" step="0.1" value="1">
      <span class="timeReadout" id="speedReadout">1.0x</span>
      <span class="zoomLabel">时间密度</span>
      <input id="zoom" type="range" min="30" max="600" step="5" value="160">
      <span class="timeReadout" id="timeReadout">0.000s</span>
    </div>
  </div>
  <div class="laneToggles" id="laneToggles"></div>
  <div class="tlBody">
    <div class="tlLabels" id="tlLabels"></div>
    <div class="tlViewport" id="tlViewport">
      <div class="tlInner" id="tlInner">
        <div class="ruler" id="ruler"></div>
        <div id="lanes"></div>
      </div>
      <div class="hoverLine" id="hoverLine"></div>
      <div class="playhead"></div>
    </div>
  </div>
  <div class="hint">拖动 / 暂停时红线处按键会高亮并同步表格 · 点击表格行可让时间轴跳到该操作 · 悬停聚焦、点击记录可单显 · 低于 200ms 按单击显示</div>
</section>

<section class="panel">
  <div class="panelHead">
    <div class="panelTitle">操作明细</div>
    <div class="filterBarRight">
      <button class="miniBtn" id="filterToggle">筛选</button>
      <span class="rowCount" id="filterSummary"></span>
      <span class="rowCount" id="rowCount"></span>
    </div>
  </div>
  <div class="tableFilters collapsed" id="tableFilters"></div>
  <div class="tableWrap" id="tableWrap">
    <table>
      <thead>
        <tr>
          <th data-sort="a">动作</th>
          <th data-sort="p">按下时间</th>
          <th data-sort="r">抬起时间</th>
          <th data-sort="d">时长</th>
        </tr>
      </thead>
      <tbody id="tbody"></tbody>
    </table>
  </div>
</section>

</div>

<div id="viewTable" class="view hidden">
<section class="panel">
  <div class="panelHead">
    <div class="panelTitle">全部操作</div>
    <div class="filterBarRight">
      <span class="rowCount" id="filterSummary2"></span>
      <span class="rowCount" id="rowCount2"></span>
    </div>
  </div>
  <div class="tableFilters" id="tableFilters2"></div>
  <div class="tableWrap tableWrapLarge" id="tableWrap2">
    <table>
      <thead>
        <tr>
          <th data-sort2="a">动作</th>
          <th data-sort2="p">按下时间</th>
          <th data-sort2="r">抬起时间</th>
          <th data-sort2="d">时长</th>
        </tr>
      </thead>
      <tbody id="tbody2"></tbody>
    </table>
  </div>
</section>
</div>

<script>
const DATA = __DATA__;
const NAMES = __NAMES__;
const ICONS = __ICONS__;

const rows = (DATA.rows || []).slice().sort((a, b) => a.p - b.p);
const total = rows.length ? Math.max.apply(null, rows.map(r => r.r)) : 0;

const COLORS = {
  attack: '#F2FF3C',
  special: '#FF9F43',
  ultimate: '#FF5E5E',
  dodge: '#4FC3F7',
  assist: '#60A5FA',
  switchPrev: '#A78BFA',
  switchNext: '#C084FC',
  chainLeft: '#22D3EE',
  chainRight: '#2DD4BF',
  chainCancel: '#FB7185',
  lockOn: '#FACC15',
  agentSkill: '#F472B6',
  dirUp: '#86EFAC',
  dirDown: '#4ADE80',
  dirLeft: '#34D399',
  dirRight: '#10B981',
  dirUpLeft: '#A7F3D0',
  dirUpRight: '#6EE7B7',
  dirDownLeft: '#22C55E',
  dirDownRight: '#16A34A',
  dirRotate: '#E879F9'
};
const DEFAULT_COLOR = '#8A9195';

const nameOf = a => NAMES[a] || a;
const iconOf = a => ICONS[a] || '';
const colorOf = a => COLORS[a] || DEFAULT_COLOR;
const fmt = v => v.toFixed(3) + 's';

const LANES = [
  { id: 'move', title: '移动操作', visible: true, actions: ['dirUp','dirDown','dirLeft','dirRight','dirUpLeft','dirUpRight','dirDownLeft','dirDownRight','dirRotate'] },
  { id: 'assist', title: '闪避与支援技', visible: true, actions: ['dodge','assist','switchPrev','switchNext','chainLeft','chainRight','chainCancel'] },
  { id: 'attack', title: '普通攻击', visible: true, actions: ['attack'] },
  { id: 'special', title: '特殊技与终极技', visible: true, actions: ['special','ultimate'] },
  { id: 'other', title: '其他', visible: true, actions: [] }
];

const visibleLanes = () => LANES.filter(lane => lane.visible);
const laneFor = action => LANES.find(lane => lane.actions.includes(action)) || LANES.find(lane => lane.actions.length === 0) || null;
const laneIndexOf = action => { const lane = laneFor(action); return lane ? visibleLanes().indexOf(lane) : -1; };

const SUB_H = 30;
const CLICK_PX = 20;
const CLUSTER_PX = 34;
const GAP_PX = 2;
const RULER_H = 26;
const PAD = 48;
const MIN_BAR = 40;
const CLICK_SEC = 0.2;

const state = { pps: 160, speed: 1, centerTime: 0, baseTime: 0, playing: false, playStart: 0, raf: 0, lastActive: null, soloAction: null, suppressClick: false };
let laneModel = [];

const viewport = document.getElementById('tlViewport');
const inner = document.getElementById('tlInner');
const labels = document.getElementById('tlLabels');
const ruler = document.getElementById('ruler');
const lanesEl = document.getElementById('lanes');
const zoom = document.getElementById('zoom');
const speed = document.getElementById('speed');
const speedReadout = document.getElementById('speedReadout');
const playBtn = document.getElementById('playBtn');
const replayBtn = document.getElementById('replayBtn');
const timeReadout = document.getElementById('timeReadout');
const tableWrap = document.getElementById('tableWrap');
const tbody = document.getElementById('tbody');

document.getElementById('meta').innerHTML =
  '<span class="chip">' + rows.length + ' 条操作</span>' +
  '<span class="chip">总长 ' + fmt(total) + '</span>';
document.getElementById('rowCount').textContent = rows.length + ' 条';

function laneColor(lane) {
  if (!lane) return DEFAULT_COLOR;
  const firstAction = lane.actions.find(a => rows.some(r => r.a === a));
  if (firstAction) return colorOf(firstAction);
  const anyRow = rows.find(r => laneFor(r.a) === lane);
  return anyRow ? colorOf(anyRow.a) : DEFAULT_COLOR;
}

function makeItem(action, p, r, count) {
  const duration = r - p;
  const kind = count > 1 ? 'cluster' : (duration < CLICK_SEC ? 'tap' : 'bar');
  const widthPx = kind === 'cluster' ? CLUSTER_PX : (kind === 'tap' ? CLICK_PX : Math.max(MIN_BAR, Math.round(duration * state.pps)));
  const title = count > 1
    ? nameOf(action) + ' ×' + count + ' · ' + fmt(p) + ' → ' + fmt(r)
    : nameOf(action) + ' · ' + fmt(p) + ' → ' + fmt(r);

  return { a: action, p: p, r: r, count: count, kind: kind, widthPx: widthPx, title: title };
}

function subtractSegments(segments, start, end) {
  const result = [];

  segments.forEach(segment => {
    if (end <= segment[0] || start >= segment[1]) {
      result.push(segment);
      return;
    }

    if (start > segment[0]) result.push([segment[0], Math.min(start, segment[1])]);
    if (end < segment[1]) result.push([Math.max(end, segment[0]), segment[1]]);
  });

  return result;
}
function movementItems(list) {
  const cardinals = ['dirUp', 'dirDown', 'dirLeft', 'dirRight'];
  const pairs = [
    ['dirUp', 'dirLeft', 'dirUpLeft'],
    ['dirUp', 'dirRight', 'dirUpRight'],
    ['dirDown', 'dirLeft', 'dirDownLeft'],
    ['dirDown', 'dirRight', 'dirDownRight']
  ];

  const bars = [];
  const items = [];

  list.forEach(row => {
    if (cardinals.includes(row.a) && (row.r - row.p) >= CLICK_SEC) {
      bars.push({ a: row.a, p: row.p, r: row.r, cuts: [] });
    } else {
      items.push(makeItem(row.a, row.p, row.r, 1));
    }
  });

  pairs.forEach(pair => {
    bars.filter(bar => bar.a === pair[0]).forEach(first => {
      bars.filter(bar => bar.a === pair[1]).forEach(second => {
        const start = Math.max(first.p, second.p);
        const end = Math.min(first.r, second.r);

        if (end - start > 0.001) {
          items.push(makeItem(pair[2], start, end, 1));
          first.cuts.push([start, end]);
          second.cuts.push([start, end]);
        }
      });
    });
  });

  bars.forEach(bar => {
    let segments = [[bar.p, bar.r]];
    bar.cuts.forEach(cut => { segments = subtractSegments(segments, cut[0], cut[1]); });
    segments.forEach(segment => {
      if (segment[1] - segment[0] > 0.001) {
        items.push(makeItem(bar.a, segment[0], segment[1], 1));
      }
    });
  });

  return items;
}
function clusterItems(list) {
  const groups = new Map();

  list.forEach(row => {
    if (!groups.has(row.a)) groups.set(row.a, []);
    groups.get(row.a).push(row);
  });

  const items = [];

  groups.forEach((group, action) => {
    group.sort((a, b) => a.p - b.p);
    let cluster = null;

    group.forEach(row => {
      if (cluster && (row.p - cluster.lastP) * state.pps < CLICK_PX + 4) {
        cluster.lastP = row.p;
        cluster.r = Math.max(cluster.r, row.r);
        cluster.count++;
      } else {
        if (cluster) items.push(cluster);
        cluster = { a: action, p: row.p, r: row.r, lastP: row.p, count: 1 };
      }
    });

    if (cluster) items.push(cluster);
  });

  return items.map(item => makeItem(item.a, item.p, item.r, item.count));
}

function collectItems(lane, laneIndex) {
  const list = rows.filter(row => laneIndexOf(row.a) === laneIndex);
  return lane.id === 'move' ? movementItems(list) : clusterItems(list);
}
function packItems(items) {
  const sorted = items.slice().sort((a, b) => (a.p - b.p) || (a.r - b.r));
  const rowEnds = [];
  const packed = [];

  sorted.forEach(item => {
    const startPx = item.p * state.pps;
    let row = rowEnds.findIndex(end => end + GAP_PX <= startPx);

    if (row < 0) {
      row = rowEnds.length;
      rowEnds.push(startPx + item.widthPx);
    } else {
      rowEnds[row] = startPx + item.widthPx;
    }

    packed.push({ a: item.a, p: item.p, r: item.r, count: item.count, kind: item.kind, widthPx: item.widthPx, title: item.title, row: row });
  });

  return { items: packed, rows: Math.max(1, rowEnds.length) };
}

function buildLaneModel() {
  return visibleLanes().map((lane, index) => {
    const packed = packItems(collectItems(lane, index));

    return {
      lane: lane,
      items: packed.items,
      height: Math.max(1, packed.rows) * SUB_H + 8
    };
  });
}
function buildToggles() {
  const host = document.getElementById('laneToggles');
  host.innerHTML = LANES.map(lane =>
    '<label><input type="checkbox" data-lane="' + lane.id + '"' + (lane.visible ? ' checked' : '') + '>' +
    '<span class="dot" style="background:' + laneColor(lane) + '"></span>' + lane.title + '</label>').join('');

  host.querySelectorAll('input[data-lane]').forEach(input => {
    input.addEventListener('change', () => {
      const lane = LANES.find(item => item.id === input.dataset.lane);
      if (!lane) return;
      lane.visible = input.checked;
      buildAll();
    });
  });
}

function buildLabels() {
  labels.innerHTML = '<div class="labelSpacer"></div>' + laneModel.map(model =>
    '<div class="laneLabel" draggable="true" data-lane="' + model.lane.id + '" style="height:' + model.height + 'px" title="拖动调整顺序">' +
      '<span class="grip">≡</span>' +
      '<span class="laneMark" style="color:' + laneColor(model.lane) + ';background:' + laneColor(model.lane) + '"></span>' +
      model.lane.title +
    '</div>').join('');

  bindLaneDrag();
}
function buildLanes() {
  let top = RULER_H;
  let html = '';

  laneModel.forEach(model => {
    const items = model.items.map(item => {
      const left = Math.round(PAD + item.p * state.pps);
      const itemTop = 4 + item.row * SUB_H;
      const icon = iconOf(item.a);
      const common = ' data-action="' + item.a + '" data-start="' + item.p + '">';

      if (item.kind === 'tap') {
        return '<div class="tap" style="left:' + (left - 10) + 'px;top:' + itemTop + 'px" title="' + item.title + '"' + common +
          (icon ? '<img src="' + icon + '" alt="">' : '') + '</div>';
      }

      if (item.kind === 'cluster') {
        return '<div class="cluster" style="left:' + (left - 10) + 'px;top:' + itemTop + 'px;--c:' + colorOf(item.a) + '" title="' + item.title + '"' + common +
          (icon ? '<img src="' + icon + '" alt="">' : '') +
          '<span class="badge">×' + item.count + '</span></div>';
      }

      return '<div class="bar" style="left:' + left + 'px;top:' + itemTop + 'px;width:' + item.widthPx + 'px;--c:' + colorOf(item.a) + '" title="' + item.title + '"' + common +
        '<span class="strip"></span>' +
        (icon ? '<img class="cap l" src="' + icon + '" alt=""><img class="cap r" src="' + icon + '" alt="">' : '') +
        '</div>';
    }).join('');

    html += '<div class="lane" style="top:' + top + 'px;height:' + model.height + 'px">' + items + '</div>';
    top += model.height;
  });

  lanesEl.innerHTML = html;
}
let dragLaneId = null;

function bindLaneDrag() {
  labels.querySelectorAll('.laneLabel').forEach(element => {
    element.addEventListener('dragstart', event => {
      dragLaneId = element.dataset.lane;
      element.classList.add('dragging');
      event.dataTransfer.effectAllowed = 'move';
      try { event.dataTransfer.setData('text/plain', dragLaneId); } catch (error) {}
    });

    element.addEventListener('dragend', () => {
      element.classList.remove('dragging');
      clearDropTargets();
      dragLaneId = null;
    });

    element.addEventListener('dragover', event => {
      event.preventDefault();
      clearDropTargets();
      element.classList.add('dropTarget');
    });

    element.addEventListener('dragleave', () => element.classList.remove('dropTarget'));

    element.addEventListener('drop', event => {
      event.preventDefault();
      const targetId = element.dataset.lane;
      const sourceId = dragLaneId || event.dataTransfer.getData('text/plain');
      clearDropTargets();

      if (!sourceId || sourceId === targetId) return;
      moveLane(sourceId, targetId);
    });
  });
}
function clearDropTargets() {
  labels.querySelectorAll('.dropTarget').forEach(element => element.classList.remove('dropTarget'));
}

function moveLane(sourceId, targetId) {
  const from = LANES.findIndex(lane => lane.id === sourceId);
  const to = LANES.findIndex(lane => lane.id === targetId);

  if (from < 0 || to < 0 || from === to) return;

  const lane = LANES.splice(from, 1)[0];
  LANES.splice(to, 0, lane);
  buildAll();
}
function buildAll() {
  clearHover();
  laneModel = buildLaneModel();
  buildToggles();
  buildLabels();
  buildLanes();
  buildRuler();
  layoutSize();
  applyPan();
  applySoloClasses();

  if (tbody.querySelector('tr[data-i]')) {
    highlight(state.centerTime, false);
  }
}

function niceStep(minPixels) {
  const raw = minPixels / state.pps;
  const steps = [0.05,0.1,0.2,0.5,1,2,5,10,15,30,60,120,300];
  return steps.find(s => s >= raw) || 600;
}

function buildRuler() {
  const step = niceStep(90);
  const count = Math.ceil((total + step) / step);
  let html = '';

  for (let i = 0; i <= count; i++) {
    const t = i * step;
    const x = Math.round(PAD + t * state.pps);
    const label = step < 1 ? t.toFixed(2) : String(Math.round(t));
    html += '<div class="tick" style="left:' + x + 'px">' + label + 's</div>';
  }

  ruler.innerHTML = html;
}

function layoutSize() {
  const width = (PAD * 2) + Math.max(total, 0.5) * state.pps;
  const height = RULER_H + laneModel.reduce((sum, model) => sum + model.height, 0);
  inner.style.width = width + 'px';
  inner.style.height = height + 'px';
  viewport.style.height = height + 'px';
}

function applyPan() {
  const width = viewport.clientWidth || 1;
  const offset = Math.round(PAD + state.centerTime * state.pps - (width / 2));
  inner.style.transform = 'translateX(' + (-offset) + 'px)';
  timeReadout.textContent = fmt(state.centerTime);
}

let drag = null;

viewport.addEventListener('pointerdown', event => {
  if (event.button !== 0) return;
  pausePlayback();
  state.suppressClick = false;
  drag = { x: event.clientX, center: state.centerTime };
  viewport.classList.add('dragging');
  viewport.setPointerCapture(event.pointerId);
});

viewport.addEventListener('pointermove', event => {
  if (!drag) return;
  const dx = event.clientX - drag.x;
  if (Math.abs(dx) > 4) state.suppressClick = true;
  state.centerTime = Math.max(0, Math.min(total, drag.center - dx / state.pps));
  applyPan();
  highlight(state.centerTime, false);
});

function endDrag(event) {
  if (!drag) return;
  drag = null;
  viewport.classList.remove('dragging');
  try { viewport.releasePointerCapture(event.pointerId); } catch (e) {}
}

viewport.addEventListener('pointerup', endDrag);
viewport.addEventListener('pointercancel', endDrag);

zoom.addEventListener('input', () => {
  const center = state.centerTime;
  state.pps = Number(zoom.value);
  buildAll();
  state.centerTime = center;
  applyPan();
});

speed.addEventListener('input', () => {
  const value = Number(speed.value);

  if (state.playing) {
    state.baseTime = state.centerTime;
    state.playStart = performance.now();
  }

  state.speed = value;
  speedReadout.textContent = value.toFixed(1) + 'x';
});

function setPlayLabel() {
  playBtn.textContent = state.playing ? '❚❚ 暂停' : '▶ 播放';
  playBtn.classList.toggle('isPlaying', state.playing);
}

const hoverLine = document.getElementById('hoverLine');

function showHoverLine(start) {
  const width = viewport.clientWidth || 1;
  const offset = Math.round(PAD + state.centerTime * state.pps - (width / 2));
  const x = Math.round(PAD + start * state.pps) - offset;

  if (x < 0 || x > width) {
    hoverLine.style.opacity = '0';
    return;
  }

  hoverLine.style.left = x + 'px';
  hoverLine.style.opacity = '1';
}

function clearHover() {
  inner.classList.remove('focusing');
  lanesEl.querySelectorAll('.focus').forEach(element => element.classList.remove('focus'));
  hoverLine.style.opacity = '0';
}

function applySoloClasses() {
  inner.classList.toggle('soloing', !!state.soloAction);
  lanesEl.querySelectorAll('[data-action]').forEach(element => {
    element.classList.toggle('solo', element.dataset.action === state.soloAction);
  });
}

lanesEl.addEventListener('mouseover', event => {
  const element = event.target.closest('[data-action]');
  if (!element) return;
  lanesEl.querySelectorAll('.focus').forEach(node => node.classList.remove('focus'));
  element.classList.add('focus');
  inner.classList.add('focusing');
  showHoverLine(Number(element.dataset.start));
});

lanesEl.addEventListener('mouseout', event => {
  if (event.relatedTarget && lanesEl.contains(event.relatedTarget)) return;
  clearHover();
});

lanesEl.addEventListener('click', event => {
  if (state.suppressClick) return;
  const element = event.target.closest('[data-action]');
  if (!element) return;
  state.soloAction = state.soloAction === element.dataset.action ? null : element.dataset.action;
  applySoloClasses();
});
function startPlayback(fromTime) {
  if (!rows.length || state.playing) return;

  let from = Number.isFinite(fromTime) ? fromTime : state.centerTime;
  if (from >= total - 1e-6) from = 0;

  state.baseTime = Math.max(0, Math.min(total, from));
  state.playing = true;
  state.playStart = performance.now();
  state.centerTime = state.baseTime;
  applyPan();
  highlight(state.baseTime);
  setPlayLabel();
  state.raf = requestAnimationFrame(tick);
}

function pausePlayback() {
  if (state.raf) {
    cancelAnimationFrame(state.raf);
    state.raf = 0;
  }

  if (!state.playing) return;

  state.playing = false;
  setPlayLabel();
}

function tick(now) {
  if (!state.playing) return;

  const t = state.baseTime + ((now - state.playStart) / 1000) * state.speed;

  if (t >= total) {
    state.centerTime = total;
    applyPan();
    pausePlayback();
    highlight(total);
    return;
  }

  state.centerTime = t;
  applyPan();
  highlight(t);
  state.raf = requestAnimationFrame(tick);
}

playBtn.addEventListener('click', () => {
  if (state.playing) {
    pausePlayback();
  } else {
    startPlayback(state.centerTime);
  }
});

replayBtn.addEventListener('click', () => {
  pausePlayback();
  state.centerTime = 0;
  applyPan();
  highlight(0);
  startPlayback(0);
});

const filterHost = document.getElementById('tableFilters');
const filterHost2 = document.getElementById('tableFilters2');
const usedActions = Array.from(new Set(rows.map(row => row.a)));
const hiddenActions = new Set();
const filterToggle = document.getElementById('filterToggle');
const filterSummary = document.getElementById('filterSummary');
let filtersOpen = false;

function setFiltersOpen(open) {
  filtersOpen = open;
  filterHost.classList.toggle('collapsed', !open);
  filterToggle.textContent = open ? '收起筛选' : '筛选';
  filterToggle.classList.toggle('isActive', open);
}

filterToggle.addEventListener('click', () => setFiltersOpen(!filtersOpen));

function buildFilters(host) {
  if (!rows.length) return;

  const groups = LANES
    .map(lane => ({ lane: lane, actions: usedActions.filter(action => laneFor(action) === lane) }))
    .filter(group => group.actions.length > 0);

  host.innerHTML = groups.map(group =>
    '<div class="filterGroup" data-lane="' + group.lane.id + '">' +
      '<label><input type="checkbox" data-master>' + group.lane.title + '</label>' +
      '<div class="filterItems">' + group.actions.map(action =>
        '<label><input type="checkbox" data-action="' + action + '">' +
        (iconOf(action) ? '<img src="' + iconOf(action) + '" alt="">' : '') +
        '<span>' + nameOf(action) + '</span></label>').join('') +
      '</div>' +
    '</div>').join('') +
    '<div class="filterActions">' +
      '<button class="miniBtn" data-all="on">全部显示</button>' +
      '<button class="miniBtn" data-all="off">全部隐藏</button>' +
    '</div>';

  bindFilterEvents(host);
}
function bindFilterEvents(host) {
  host.querySelectorAll('.filterGroup').forEach(group => {
    const master = group.querySelector('input[data-master]');
    const items = Array.from(group.querySelectorAll('input[data-action]'));

    master.addEventListener('change', () => {
      items.forEach(item => { item.checked = master.checked; });
      items.forEach(item => {
        if (item.checked) hiddenActions.delete(item.dataset.action);
        else hiddenActions.add(item.dataset.action);
      });
      refreshFilters();
    });

    items.forEach(item => item.addEventListener('change', () => {
      if (item.checked) hiddenActions.delete(item.dataset.action);
      else hiddenActions.add(item.dataset.action);
      refreshFilters();
    }));
  });

  host.querySelectorAll('button[data-all]').forEach(button => {
    button.addEventListener('click', () => {
      const show = button.dataset.all === 'on';
      usedActions.forEach(action => {
        if (show) hiddenActions.delete(action);
        else hiddenActions.add(action);
      });
      refreshFilters();
    });
  });
}

function refreshFilters() {
  [filterHost, filterHost2].forEach(host => {
    if (!host) return;
    host.querySelectorAll('.filterGroup').forEach(group => {
      const items = Array.from(group.querySelectorAll('input[data-action]'));
      items.forEach(item => { item.checked = !hiddenActions.has(item.dataset.action); });
      const master = group.querySelector('input[data-master]');
      if (master) {
        const checked = items.filter(item => item.checked).length;
        master.checked = checked === items.length;
        master.indeterminate = checked > 0 && checked < items.length;
      }
    });
  });

  renderTable();
  renderTable2();
}

function buildTableFilters() {
  buildFilters(filterHost);
}

function buildTableFilters2() {
  buildFilters(filterHost2);
}
let sortKey = 'p';
let ascending = true;

function filteredRows() {
  return rows
    .map((row, index) => ({ row, index }))
    .filter(item => !hiddenActions.has(item.row.a))
    .sort((a, b) => {
      const va = sortKey === 'd' ? (a.row.r - a.row.p) : a.row[sortKey];
      const vb = sortKey === 'd' ? (b.row.r - b.row.p) : b.row[sortKey];
      const cmp = typeof va === 'number' ? va - vb : String(va).localeCompare(String(vb));
      return ascending ? cmp : -cmp;
    });
}

function renderTableRows(body, data) {
  if (data.length === 0) {
    body.innerHTML = '<tr><td colspan="4" class="empty">没有符合筛选条件的操作</td></tr>';
    return;
  }

  body.innerHTML = data.map(item => {
    const icon = iconOf(item.row.a);
    return '<tr data-i="' + item.index + '">' +
      '<td class="cellAction">' + (icon ? '<img src="' + icon + '" alt="">' : '') +
        '<span>' + nameOf(item.row.a) + '</span></td>' +
      '<td class="num">' + fmt(item.row.p) + '</td>' +
      '<td class="num">' + fmt(item.row.r) + '</td>' +
      '<td class="num">' + fmt(item.row.r - item.row.p) + '</td>' +
    '</tr>';
  }).join('');
}

function renderTable() {
  filterSummary.textContent = hiddenActions.size > 0 ? '已隐藏 ' + hiddenActions.size + ' 个动作' : '';
  renderTableRows(tbody, filteredRows());
  state.lastActive = null;
  highlight(state.centerTime, false);
}

function renderTable2() {
  const body = document.getElementById('tbody2');
  if (!body) return;

  const data = filteredRows();
  const summary = document.getElementById('filterSummary2');
  const count = document.getElementById('rowCount2');

  if (summary) summary.textContent = hiddenActions.size > 0 ? '已隐藏 ' + hiddenActions.size + ' 个动作' : '';
  if (count) count.textContent = data.length + ' 条';

  renderTableRows(body, data);
}

const tableRows = () => tbody.querySelectorAll('tr[data-i]');

tbody.addEventListener('click', event => {
  const rowElement = event.target.closest('tr[data-i]');
  if (!rowElement) return;

  const row = rows[Number(rowElement.dataset.i)];
  if (!row) return;

  pausePlayback();
  state.centerTime = Math.max(0, Math.min(total, row.p));
  applyPan();
  highlight(state.centerTime, true);
});

const tbody2 = document.getElementById('tbody2');

tbody2.addEventListener('click', event => {
  const rowElement = event.target.closest('tr[data-i]');
  if (!rowElement) return;

  const row = rows[Number(rowElement.dataset.i)];
  if (!row) return;

  pausePlayback();
  state.centerTime = Math.max(0, Math.min(total, row.p));
  setView('overview');
  applyPan();
  highlight(state.centerTime, true);
});

const viewOverview = document.getElementById('viewOverview');
const viewTablePanel = document.getElementById('viewTable');

function setView(name) {
  const overview = name !== 'table';
  viewOverview.classList.toggle('hidden', !overview);
  viewTablePanel.classList.toggle('hidden', overview);

  document.querySelectorAll('.viewTab').forEach(tab => {
    tab.classList.toggle('active', tab.dataset.view === (overview ? 'overview' : 'table'));
  });

  if (overview) {
    applyPan();
  } else {
    renderTable2();
  }
}

document.querySelectorAll('.viewTab').forEach(tab => {
  tab.addEventListener('click', () => setView(tab.dataset.view));
});

document.querySelectorAll('th[data-sort2]').forEach(th => {
  th.addEventListener('click', () => {
    const key = th.dataset.sort2;
    if (sortKey === key) {
      ascending = !ascending;
    } else {
      sortKey = key;
      ascending = true;
    }
    renderTable();
    renderTable2();
    if (state.playing) highlight(state.centerTime);
  });
});
function highlight(t, scroll) {
  const trs = tableRows();
  let first = null;
  const keys = [];

  trs.forEach(tr => {
    const row = rows[Number(tr.dataset.i)];
    const active = t >= row.p - 1e-6 && t <= row.r + 1e-6;
    tr.classList.toggle('active', active);

    if (active) {
      keys.push(tr.dataset.i);
      if (!first) first = tr;
    }
  });

  const key = keys.join(',');
  if (key === state.lastActive) return;

  state.lastActive = key;

  if (scroll !== false && first) {
    const top = first.offsetTop - (tableWrap.clientHeight / 2) + (first.offsetHeight / 2);
    tableWrap.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
  }
}

document.querySelectorAll('th[data-sort]').forEach(th => {
  th.addEventListener('click', () => {
    const key = th.dataset.sort;
    if (sortKey === key) {
      ascending = !ascending;
    } else {
      sortKey = key;
      ascending = true;
    }
    renderTable();
    renderTable2();
    if (state.playing) highlight(state.centerTime);
  });
});

window.addEventListener('resize', applyPan);

const themeBtn = document.getElementById('themeBtn');

function applyTheme(theme) {
  document.body.dataset.theme = theme;
  themeBtn.textContent = theme === 'light' ? '暗色' : '亮色';
  try { localStorage.setItem('zzz-activity-theme', theme); } catch (e) {}
}

let savedTheme = 'dark';
try { savedTheme = localStorage.getItem('zzz-activity-theme') || 'dark'; } catch (e) {}
applyTheme(savedTheme);

themeBtn.addEventListener('click', () => {
  applyTheme(document.body.dataset.theme === 'light' ? 'dark' : 'light');
});
if (!rows.length) {
  lanesEl.innerHTML = '<div class="empty" style="position:absolute;left:0;top:40px;width:100%">没有可显示的操作</div>';
  viewport.style.height = '90px';
  inner.style.height = '90px';
  tbody.innerHTML = '<tr><td colspan="4" class="empty">没有可显示的操作</td></tr>';
  playBtn.disabled = true;
  playBtn.style.opacity = '.4';
} else {
  buildAll();
  setFiltersOpen(false);
  buildTableFilters();
  buildTableFilters2();
  refreshFilters();
  setView('overview');
}
</script>
</body>
</html>
""";
}
