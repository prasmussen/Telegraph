package main

import (
    "flag"
    "fmt"
    "io/ioutil"
    "net/http"
    "net"
    "log"
    "sync"
    "time"
)


func NewTelegraph() *Telegraph {
    return &Telegraph{
        transmission: make(chan string, 0),
        subscribers: make(map[string]*Subscriber, 0),
        subLock: &sync.Mutex{},
    }
}

type Telegraph struct {
    transmission chan string
    subscribers map[string]*Subscriber
    subLock *sync.Mutex
}

func (t *Telegraph) ListenForSubscribers(subAddr string) {
    listener, err := net.Listen("tcp", subAddr)
    if err != nil {
        log.Fatalln(err)
    }

    for {
        conn, err := listener.Accept()
        if err != nil {
            log.Println(err)
            continue
        }
        t.SubscriberConnected(&Subscriber{conn: conn})
    }
}

func (t *Telegraph) SubscriberKeepAlive() {
    for {
        t.Transmit("PING")
        time.Sleep(60e9)
    }
}

func (t *Telegraph) SubscriberConnected(sub *Subscriber) {
    log.Printf("Subscriber '%s' connected\n", sub.Identifier())
    t.subLock.Lock()
    defer t.subLock.Unlock()
    t.subscribers[sub.Identifier()] = sub
}

func (t *Telegraph) SubscriberDisconnected(sub *Subscriber) {
    log.Printf("Subscriber '%s' disconnected\n", sub.Identifier())
    t.subLock.Lock()
    defer t.subLock.Unlock()
    delete(t.subscribers, sub.Identifier())
}

func (t *Telegraph) Transmit(msg string) {
    missing := make([]*Subscriber, 0)
    t.subLock.Lock()
    for _, sub := range t.subscribers {
        if err := sub.SendMessage(msg); err != nil {
            missing = append(missing, sub)
        } else {
            log.Printf("Sending '%s' to '%s'\n", msg, sub.Identifier())
        }
    }
    t.subLock.Unlock()

    for _, sub := range missing {
        t.SubscriberDisconnected(sub)
    }
}

func (t *Telegraph) Start(transAddr, subAddr string) {
    log.Printf("Listening for subscribers on '%s'\n", subAddr)
    go t.ListenForSubscribers(subAddr)
    go t.SubscriberKeepAlive()

    log.Printf("Listening for transmissions on '%s'\n", transAddr)
    handler := NewHttpTransmissionHandler(t.transmission)
    go handler.ListenForTransmissions(transAddr)

    log.Println("Ready to receive transmissions...")
    for {
        t.Transmit(<-t.transmission)
    }
}

type Subscriber struct {
    conn net.Conn
}

func (sub *Subscriber) Identifier() string {
    return sub.conn.RemoteAddr().String()
}

func (sub *Subscriber) SendMessage(msg string) error {
    _, err := sub.conn.Write([]byte(fmt.Sprintf("%s\n", msg)))
    if err != nil {
        return err
    }
    return nil
}

func NewHttpTransmissionHandler(c chan string) *HttpTransmissionHandler {
    return &HttpTransmissionHandler{transmission: c}
}

type HttpTransmissionHandler struct {
    transmission chan string
}

func (handler *HttpTransmissionHandler) ServeHTTP(w http.ResponseWriter, req *http.Request) {
    log.Printf("%s %-4s %s\n", req.RemoteAddr, req.Method, req.URL.RawPath)

    if req.Method != "POST" || req.URL.RawPath != "/transmission" {
        http.Error(w, "Not found", http.StatusNotFound)
        return
    }

    body, err := ioutil.ReadAll(req.Body)
    if err != nil || len(body) == 0 {
        http.Error(w, "Invalid request", http.StatusBadRequest)
        return
    }
    handler.transmission<- string(body)
    w.WriteHeader(200)
}

func (handler *HttpTransmissionHandler) ListenForTransmissions(addr string) {
    err := http.ListenAndServe(addr, handler)
    if err != nil {
        log.Fatalln(err)
    }
}


func main() {
    transAddr := flag.String("l", "localhost:9090", "Address to listen for http transmissions")
    subAddr := flag.String("L", "localhost:9091", "Address to listen for clients")
    flag.Parse()

    t := NewTelegraph()
    t.Start(*transAddr, *subAddr)
}
