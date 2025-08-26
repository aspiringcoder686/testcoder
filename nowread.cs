import React, { useEffect, useMemo, useState } from 'react'
import { getEntities, getDashboard, getSqlScript,  getHibernate, compare } from './api' // getHibernate handled inside HibernatePanel
import Sidebar from './components/Sidebar'
import EntityTable from './components/EntityTable'
import RightPanel from './components/RightPanel'
import HibernatePanel from './components/HibernatePanel'
import PlaceholdersPanel from './components/PlaceholdersPanel'
import TemplateQueriesPanel from './components/TemplateQueriesPanel'
import EntityDashboardPanel from './components/EntityDashboardPanel'



import './styles.css'

const TABS = [
    'Comparator',
    'SQL Script',
    'Hibernate',
    'Entity Dashboard',
    'Placeholders',
    'Template Queries'
]

export default function App() {
    // Sidebar list
    const [entities, setEntities] = useState([]) // [{ name, entityName }]
    // Selection (entity/domain) + name (used by Hibernate)
    const [pair, setPair] = useState({ entity: '', domain: '', name: '' })
    // Dashboard metadata
    const [dashboard, setDashboard] = useState([])
    // Tabs
    const [activeTab, setActiveTab] = useState('Comparator')

    // UI helpers
    const [filter, setFilter] = useState('')
    const [selectedEntityIncludes, setSelectedEntityIncludes] = useState([])

    // SQL tab data
    const [sql, setSql] = useState('')
    const [loading, setLoading] = useState(false)
    const [err, setErr] = useState('')

    const [rows, setRows] = useState([])

    // Load sidebar + dashboard
    useEffect(() => {
        getEntities().then(setEntities)
        getDashboard().then(setDashboard)
    }, [])

    // Load SQL script (once on first entry to tab)
    useEffect(() => {
        async function loadSql() {
            if (activeTab !== 'SQL Script') return
            if (sql) return
            setLoading(true)
            try { setSql((await getSqlScript()) || '') }
            catch (e) { setErr(String(e)) }
            finally { setLoading(false) }
        }
        loadSql()
    }, [activeTab, sql])

    // When switching to Hibernate, default the name box to the selected entity (if empty)
    useEffect(() => {
        if (activeTab === 'Hibernate' && !pair.name && pair.domain) {
            setPair(p => ({ ...p, name: p.domain }))
        }
    }, [activeTab, pair.entity]) // do not add pair.name here; we only want this to fill once

    // =========================
    // Build ENTITY-ONLY rows for the table (no domain compare)
    // =========================
    const orderedRows = useMemo(() => {
        if (!pair.entity) return []

        const A = v => (Array.isArray(v) ? v : (v ? [v] : []))
        const S = v => (typeof v === 'string' ? v : (v == null ? '' : String(v)))
        const lc = s => S(s).toLowerCase()

        // normalize names for fuzzy matching (AM*, *Dto, etc.)
        const norm = s => S(s)
            .replace(/^am|^dm/i, '')
            .replace(/dto$|model$|entity$|view$/i, '')
            .toLowerCase()

        const findDefFuzzy = (name) => {
            let best = null, score = -1
            const rn = norm(name)
            for (const d of (dashboard || [])) {
                const dn = norm(d?.name || '')
                const sc = rn === dn ? 100 : rn.endsWith(dn) || rn.includes(dn) ? 80 : dn.includes(rn) ? 60 : 0
                if (sc > score) { best = d; score = sc }
            }
            return best
        }

        const baseDef = findDefFuzzy(pair.entity)
        if (!baseDef) return []

        const rows = []
        // root properties (mark PKs)
        for (const p of A(baseDef.properties)) {
            rows.push({
                match: 'Property',
                entityPath: p.name,
                entitySide: { name: p.name, type: p.type },
                keyType: p.isPrimary ? 'PK' : undefined
            })
        }

        const findDefByName = (name) =>
            (dashboard || []).find(d => lc(d?.name) === lc(name)) || null

        const isFkRel = (t) => ['many-to-one', 'one-to-one', 'reference'].includes(lc(t))

        const MAX_DEPTH = 2
        const visited = new Set()

        function walk(def, prefix = '', depth = 0) {
            if (!def || depth >= MAX_DEPTH) return
            for (const rel of A(def.relationships)) {
                const path = prefix ? `${prefix}.${rel.name}` : rel.name
                const k = path.toLowerCase()
                if (visited.has(k)) continue
                visited.add(k)

                const target = findDefByName(rel.class)

                // navigation row
                rows.push({
                    match: 'Navigation',
                    entityPath: path,
                    entitySide: { name: rel.name, type: rel.class },
                    includeSuggestion: path,
                    keyType: isFkRel(rel.type) ? 'FK' : undefined,
                    navTargetTable: target?.table || rel.class || undefined
                })

                // child properties
                for (const p of A(target?.properties)) {
                    rows.push({
                        match: 'Property',
                        entityPath: `${path}.${p.name}`,
                        entitySide: { name: `${path}.${p.name}`, type: p.type },
                        includeSuggestion: path
                    })
                }

                if (target) walk(target, path, depth + 1)
            }
        }

        walk(baseDef)
        return rows
    }, [dashboard, pair.entity])

    // Sidebar selection highlight: use the same composite key the sidebar uses
    const selectedKey = useMemo(
        () => `${(pair.entity || '').trim()}||${(pair.domain || '').trim()}`.toLowerCase(),
        [pair.entity, pair.domain]
    )

   const setPairName = (name) =>  setPair(prev => ({ ...prev, name }));

    return (
        <div className="layout">
            {/* Sidebar */}
            <aside className="sidebar">
                <Sidebar
                    entities={entities}
                    selectedKey={selectedKey}
                    onPick={async (item) => {
                        // item: { name, entityName }
                        setPair({
                            entity: item.entityName,   // EF/Entity side
                            domain: item.name,         // Domain side
                            name: item.name            // For Hibernate lookup
                        })
                        setSelectedEntityIncludes([])
                        setActiveTab('Comparator')
                        
                        try {
                            const rows = await compare(item.entityName, item.name)
                            setRows(rows)   // assuming you have setRows from useState([])
                        } catch (err) {
                            console.error('Compare failed:', err)
                        }
                    }}
                />

            </aside>

            <main className="main">
                <header className="hdr">
                    <h1>{pair.entity ? pair.entity : 'Select an entity'}</h1>

                    <div className="hdr-row">
                        <div className="tabs">
                            {TABS.map(t => (
                                <button
                                    key={t}
                                    className={'tab' + (t === activeTab ? ' active' : '')}
                                    onClick={() => setActiveTab(t)}
                                    disabled={t === 'Hibernate' && !pair.name}
                                    title={t === 'Hibernate' && !pair.name ? 'Pick a type first' : t}
                                >
                                    {t}
                                </button>
                            ))}
                        </div>
                        
                    </div>
                </header>

                <section className="content-3col">
                    <div className="center">
                        {activeTab === 'Comparator' && (
                            <>
                                <div className="table"><div className="title">Entity</div></div>
                                <EntityTable
                                    rows={orderedRows.length ? orderedRows : rows}
                                    filter={filter}
                                    onSelectEntityIncludes={setSelectedEntityIncludes}
                                />

                            </>
                        )}

                        {activeTab === 'SQL Script' && (
                            <div className="pad">
                                <div className="title">SQL Server Script (all tables)</div>
                                {loading ? <div>Loading...</div> : (
                                    <pre className="code">{sql || (err ? String(err) : '(empty)')}</pre>
                                )}
                            </div>
                        )}

                        {activeTab === 'Hibernate' && (
                                         <HibernatePanel name={pair.name}             
                                       setName={setPairName}         
                                       dashboard={dashboard}
             />
                        )}

                        {activeTab === 'Entity Dashboard' && (
                            <div className="dash">
                                <EntityDashboardPanel pair={pair} dashboard={dashboard} />
                            </div>
                        )}

                        {activeTab === 'Placeholders' && (
                            <PlaceholdersPanel pair={pair} dashboard={dashboard} />
                        )}

                        {activeTab === 'Template Queries' && (
                            <TemplateQueriesPanel />
                        )}
                    </div>

                    {/* Right-side query generator uses entity + includes + dashboard */}
                    <RightPanel
                        entityName={pair.entity}
                        includes={selectedEntityIncludes}
                        dashboard={dashboard}
                    />
                </section>
            </main>
        </div>
    )
}
