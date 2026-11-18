// Imports the effect hook that reacts to client-side route changes.
import { useEffect } from 'react';
// Imports the current location so scroll behavior follows navigation.
import { useLocation } from 'react-router-dom';

// Resets the page scroll position whenever the route path changes.
export default function ScrollToTop() {
  // Reads the current route pathname from React Router.
  const { pathname } = useLocation();

  // Moves the viewport to the top after navigation to a new route.
  useEffect(() => {
    // Avoids retaining a previous page's scroll position.
    window.scrollTo(0, 0);
  }, [pathname]);

  // Renders nothing because this component only provides a route side effect.
  return null;
}