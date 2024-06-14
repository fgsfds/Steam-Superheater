import { useEffect, useState } from 'react';
import './App.css';

interface FixesStats {
    GamesCount: number;
    FixesCount: number;
    FixesLists: FixesLists[];
    NoIntroFixes: string[];
}
interface FixesLists {
    Game: string;
    Fixes: string[];
}

function App() {
    const [fixes, setFixesStats] = useState<FixesStats[]>([]);

    useEffect(() => {
        getFixesStats();
    }, []);

    return (
        <div>
            <img src="logo.png" width="300"></img>
            <h1 id="tabelLabel">Steam Superheater</h1>
            <h4 id="tabelLabel">Fix old and broken Steam games with a couple of clicks</h4>

            <div>
                <a className="big-font" href="https://github.com/fgsfds/Steam-Superheater/releases/latest">
                    Download from GitHub
                </a>
            </div>

            <div>

            <br/>
            <br/>

                <a href="https://github.com/fgsfds/Steam-Superheater">
                    <img className="with-margin" src="github.png" height="100"></img>
                </a>

                <a href="https://discord.gg/mWvKyxR4et">
                    <img className="with-margin" src="discord.png" height="100"></img>
                </a>
                
            </div>

            <br />
            <br />

            <h2>Currently has {fixes.FixesCount} fixes for {fixes.GamesCount} games</h2>
            <h4>(not counting no intro fixes)</h4>

            <br />

            <div style={{ display: "inline-block", textAlign: "left" }}>{fixes.FixesLists?.map(f =>

                <div >
                    <h3>{f.Game}</h3>
                    {f.Fixes.map(ff => <div>&nbsp; &nbsp; &nbsp; &nbsp; • {ff}</div>)}
                </div>

            )}</div>

            <br />
            <br />

            <div><b>No Intro Fixes for:</b> {fixes.NoIntroFixes?.join(', ')}</div>
                
        </div>
    );

    async function getFixesStats() {
        const response = await fetch('api/fixes/stats');
        const data = await response.json();
        setFixesStats(data);
    }
}

export default App;