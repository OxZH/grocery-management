(function(){
    // Poll for new orders every 30 seconds
    let lastCheck = new Date(0).toISOString();
    async function poll(){
        try{
            const res = await fetch('/Orders/Notifications?since='+encodeURIComponent(lastCheck));
            if (res.ok){
                const data = await res.json();
                if (data.newOrders && data.newOrders.length>0){
                    data.newOrders.forEach(o=>{
                        console.info('New order', o);
                        notify('New order '+o.Id+' total:'+o.Total);
                    });
                    lastCheck = new Date().toISOString();
                }
            }
        }catch(e){ console.warn(e); }
    }

    function notify(msg){
        if (window.Notification && Notification.permission === 'granted'){
            new Notification('Orders', { body: msg });
        } else if (window.Notification && Notification.permission !== 'denied'){
            Notification.requestPermission().then(p=>{ if (p==='granted') new Notification('Orders', { body: msg }); });
        } else {
            alert(msg);
        }
    }

    setInterval(poll, 30000);
    // initial
    setTimeout(poll, 2000);
})();
