import { useState } from "react";
import { createUseStyles } from "react-jss";
import getFlag from "../../../lib/getFlag";
import { launchGame } from "../../../services/games";
import AuthenticationStore from "../../../stores/authentication";
import useButtonStyles from "../../../styles/buttonStyles";
import ActionButton from "../../actionButton";

const useStyles = createUseStyles({
  buttonWrapper: {
    width: '100%',
    maxWidth: '200px',
  },
  button: {
    width: '100%',
    paddingTop: '2px',
    paddingBottom: '4px',
  },
})

/**
 * Play button
 * @param {{placeId: number}} props 
 * @returns 
 */
const PlayButton = props => {
  const [error, setError] = useState(null);
  const auth = AuthenticationStore.useContainer();
  const s = useStyles();
  const buttonStyles = useButtonStyles();
  const onClick = async e => {
    if (getFlag('launchUsingEsURI', false)) {
      e && e.preventDefault && e.preventDefault();
      if (!auth.isAuthenticated) {
        window.location.href = '/Login';
        return
      }
      launchGame({
        placeId: props.placeId,
      }).catch(e => {
        // todo: modal
        setError(e.message);
      });
    } else if (getFlag('launchUsingEsWeb', false)) {
      window.location.href = '/RobloxApp/Play?placeId=' + props.placeId;
    } else if (window.innerWidth <= 768) { // Assuming mobile if width is less than or equal to 768px
      window.location.href = `https://www.moonic.wtf/games/start?placeId=${props.placeId}`;
    } else {
      // TODO: Roblox URI handling here (is this even possible?)
      const req = await fetch("/game/get-join-script?placeId=" + props.placeId)
      if (req.status !== 200) {
        setError('Failed to get join script');
        return;
      }
      // define json so i can do json.joinScriptUrl
      const json = await req.json();
      if (json && json.joinScriptUrl && json.prefix) {
        // open uri!
        try {
          // we must URL encode the URI eventually somehow
          const finalURI = `${json.prefix}${json.joinScriptUrl}`;
          console.log(`trying to open ${finalURI}`);
          window.open(finalURI, '_blank');
        } catch (error) {
          console.log(`caught: ${error}`)
          setError("Failed to launch Bootstrapper. This is an issue with your browser, or you don't have Moonic installed.");
        }
      } else {
        setError('Failed to get join script');
      }
    }
  }

  return <div className='row'>
    <div className={'col-12 mx-auto ' + s.buttonWrapper}>
      {error && <p className='text-danger mb-1 mt-1'>{error}</p>}
      <ActionButton label='Play' className={s.button + ' ' + buttonStyles.buyButton} onClick={onClick}></ActionButton>
    </div>
  </div>
}

export default PlayButton;