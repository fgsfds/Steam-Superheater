import { useEffect } from 'react';
import './App.css';

function App() {

    useEffect(() => {
    }, []);

    return (
        <div>
            <img src="logo.png" width="200"></img>
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
                    <img className="with-margin" src="https://cdn-icons-png.flaticon.com/512/25/25231.png" height="100"></img>
                </a>

                <a href="https://discord.gg/mWvKyxR4et">
                    <img className="with-margin" src="https://static.vecteezy.com/system/resources/previews/023/741/066/original/discord-logo-icon-social-media-icon-free-png.png" height="100"></img>
                </a>
                
            </div>

        </div>
    );
}

export default App;