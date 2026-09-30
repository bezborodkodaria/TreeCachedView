const $ = id => document.getElementById(id);
const branches = new Map();
const cache = new Map();
let selectedDb = null;
let selectedCache = null;

async function request(url, options) {
  const response = await fetch(url, options);
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.error || `Request failed (${response.status})`);
  return body;
}

function message(text = '') { $('message').textContent = text; }
function key(id) { return String(id); }

async function fetchBranch(parentId = null, more = false) {
  const branchKey = key(parentId);
  const branch = branches.get(branchKey) || { nodes: [], expanded: true, hasMore: false };
  const offset = more ? branch.nodes.length : 0;
  const query = new URLSearchParams({ offset });
  if (parentId !== null) query.set('parentId', parentId);
  const data = await request(`/api/tree?${query}`);
  branch.nodes = more ? [...branch.nodes, ...data.nodes] : data.nodes;
  branch.hasMore = data.hasMore;
  branches.set(branchKey, branch);
  renderDb();
}

function row(container, depth, label, selected, deleted, onSelect, toggleLabel, onToggle) {
  const line = document.createElement('div');
  line.className = `row${selected ? ' selected' : ''}${deleted ? ' deleted' : ''}`;
  line.style.paddingLeft = `${depth * 20}px`;
  const toggle = document.createElement('button');
  toggle.className = 'toggle';
  toggle.textContent = toggleLabel || '';
  toggle.disabled = !onToggle;
  if (onToggle) toggle.onclick = onToggle;
  const name = document.createElement('button');
  name.className = 'label';
  name.textContent = label;
  name.onclick = onSelect;
  line.append(toggle, name);
  container.append(line);
  return line;
}

function renderDb() {
  const tree = $('db-tree');
  tree.replaceChildren();
  function visit(parentId, depth) {
    const branch = branches.get(key(parentId));
    if (!branch) return;
    for (const node of branch.nodes) {
      const childBranch = branches.get(key(node.id));
      row(tree, depth, node.value, selectedDb === node.id, false, () => {
        selectedDb = node.id;
        $('load').disabled = false;
        renderDb();
      }, node.hasChildren ? (childBranch?.expanded ? '▾' : '▸') : '', node.hasChildren ? async () => {
        try {
          if (!childBranch) await fetchBranch(node.id);
          else { childBranch.expanded = !childBranch.expanded; renderDb(); }
        } catch (error) { message(error.message); }
      } : null);
      if (childBranch?.expanded) visit(node.id, depth + 1);
    }
    if (branch.hasMore) {
      const more = document.createElement('button');
      more.className = 'secondary';
      more.style.marginLeft = `${depth * 20 + 26}px`;
      more.textContent = 'Load more';
      more.onclick = () => fetchBranch(parentId, true).catch(error => message(error.message));
      tree.append(more);
    }
  }
  visit(null, 0);
}

function isDeleted(node) {
  return node.path.some(id => cache.get(key(id))?.deleted);
}

function pendingCount() {
  return [...cache.values()].filter(node =>
    (node.deleted && !node.appliedDelete) || (node.isNew && !isDeleted(node)) ||
    (!isDeleted(node) && node.value !== node.original)).length;
}

function acceptAppliedChanges(created) {
  const previousSelection = selectedCache;
  const updated = new Map();

  for (const node of cache.values()) {
    const deleted = isDeleted(node);
    const oldId = key(node.id);
    node.path = node.path.map(id => created[key(id)] ?? id);
    if (typeof node.parentId === 'string') node.parentId = created[node.parentId] ?? node.parentId;
    if (created[oldId]) node.id = created[oldId];
    node.original = node.value;
    node.isNew = false;
    if (deleted) {
      node.deleted = true;
      node.appliedDelete = true;
    }
    updated.set(key(node.id), node);
  }

  cache.clear();
  for (const [id, node] of updated) cache.set(id, node);
  selectedCache = created[previousSelection] ? key(created[previousSelection]) : previousSelection;
}

function renderCache() {
  const tree = $('cache-tree');
  tree.replaceChildren();
  const nodes = [...cache.values()];
  if (!nodes.length) {
    const empty = document.createElement('div');
    empty.className = 'empty';
    empty.textContent = 'No elements loaded yet';
    tree.append(empty);
  }
  const children = new Map();
  for (const node of nodes) {
    const ancestors = node.path.slice(0, -1).reverse();
    const parent = ancestors.find(id => cache.has(key(id)));
    const group = key(parent ?? 'root');
    if (!children.has(group)) children.set(group, []);
    children.get(group).push(node);
  }
  function visit(parent, depth) {
    for (const node of children.get(key(parent)) || []) {
      const deleted = isDeleted(node);
      const line = row(tree, depth, node.value, selectedCache === key(node.id), deleted, () => {
        selectedCache = key(node.id);
        renderCache();
      });
      if (deleted) {
        const badge = document.createElement('span');
        badge.className = 'badge';
        badge.textContent = 'deleted';
        line.append(badge);
      } else if (node.isNew || node.value !== node.original) {
        const badge = document.createElement('span');
        badge.textContent = '●';
        badge.title = 'Pending change';
        line.append(badge);
      }
      visit(node.id, depth + 1);
    }
  }
  visit('root', 0);
  const current = cache.get(selectedCache);
  $('editor').hidden = !current;
  if (current) {
    $('value').value = current.value;
    $('value').disabled = isDeleted(current);
    $('save').disabled = isDeleted(current);
    $('child-value').disabled = isDeleted(current);
    $('add').disabled = isDeleted(current);
    $('delete').disabled = isDeleted(current);
  }
  const count = pendingCount();
  $('pending').textContent = count ? `${count} pending change${count === 1 ? '' : 's'}` : 'No pending changes';
  $('apply').disabled = count === 0;
}

$('load').onclick = async () => {
  if (selectedDb === null) return;
  try {
    if (!cache.has(key(selectedDb))) {
      const node = await request(`/api/cache/load/${selectedDb}`, { method: 'POST' });
      cache.set(key(node.id), { ...node, original: node.value, deleted: false, isNew: false });
    }
    selectedCache = key(selectedDb);
    renderCache();
    message();
  } catch (error) { message(error.message); }
};

$('save').onclick = () => {
  const node = cache.get(selectedCache);
  const value = $('value').value.trim();
  if (!node || isDeleted(node) || !value) return message('Enter a value.');
  node.value = value;
  renderCache();
  message();
};

$('add').onclick = () => {
  const parent = cache.get(selectedCache);
  const value = $('child-value').value.trim();
  if (!parent || isDeleted(parent) || !value) return message('Enter a child value.');
  const id = `new-${crypto.randomUUID()}`;
  cache.set(id, { id, parentId: parent.id, path: [...parent.path, id], value, original: null, deleted: false, isNew: true });
  selectedCache = id;
  renderCache();
  message();
};

$('delete').onclick = () => {
  const node = cache.get(selectedCache);
  if (!node || isDeleted(node)) return;
  node.deleted = true;
  renderCache();
  message();
};

$('apply').onclick = async () => {
  const nodes = [...cache.values()];
  const deleted = nodes.filter(node => node.deleted && !node.isNew).map(node => node.id);
  const edited = nodes.filter(node => !node.isNew && !isDeleted(node) && node.value !== node.original)
    .map(node => ({ id: node.id, value: node.value }));
  const added = nodes.filter(node => node.isNew && !isDeleted(node)).map(node => ({
    tempId: node.id,
    parentId: typeof node.parentId === 'number' ? node.parentId : null,
    parentTempId: typeof node.parentId === 'string' ? node.parentId : null,
    value: node.value
  }));
  try {
    const result = await request('/api/cache/apply', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ deleted, edited, added })
    });
    acceptAppliedChanges(result.created);
    selectedDb = null;
    $('load').disabled = true;
    branches.clear();
    await fetchBranch();
    renderCache();
    message('Changes applied. Loaded elements remain in the cache.');
  } catch (error) { message(error.message); }
};

$('reset').onclick = async () => {
  try {
    await request('/api/reset', { method: 'POST' });
    cache.clear();
    branches.clear();
    selectedCache = null;
    selectedDb = null;
    $('load').disabled = true;
    await fetchBranch();
    renderCache();
    message('Sample data restored.');
  } catch (error) { message(error.message); }
};

fetchBranch().catch(error => message(error.message));
renderCache();
